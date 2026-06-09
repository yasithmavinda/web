using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using TaskFlow.API.Hubs;
using TaskFlow.API.Middleware;
using TaskFlow.Application;
using TaskFlow.Infrastructure;
using TaskFlow.Infrastructure.Security;

// ════════════════════════════════════════════════════════
// BOOTSTRAP
// ════════════════════════════════════════════════════════
var builder = WebApplication.CreateBuilder(args);

// ── 1. Serilog Structured Logging ─────────────────────
builder.Host.UseSerilog((ctx, services, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services)
       .Enrich.FromLogContext()
       .Enrich.WithProperty("Application", "TaskFlow.API")
       .WriteTo.Console()
       .WriteTo.File("logs/taskflow-.log", rollingInterval: RollingInterval.Day,
           retainedFileCountLimit: 30));

// ── 2. Layer DI Registration ──────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── 3. JWT Authentication ──────────────────────────────
var jwtSettings  = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
var accessKeyBytes = Encoding.UTF8.GetBytes(jwtSettings.AccessSecret);

builder.Services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opts =>
{
    opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSettings.Issuer,
        ValidAudience            = jwtSettings.Audience,
        IssuerSigningKey         = new SymmetricSecurityKey(accessKeyBytes),
        ClockSkew                = TimeSpan.FromSeconds(30),
        NameClaimType            = "uid",
    };

    // Allow JWT via query string for SignalR WebSocket connections
    opts.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var token = ctx.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                ctx.Token = token;
            return Task.CompletedTask;
        },
        OnChallenge = ctx =>
        {
            ctx.HandleResponse();
            ctx.Response.StatusCode  = 401;
            ctx.Response.ContentType = "application/json";
            return ctx.Response.WriteAsync(
                """{"success":false,"message":"Authentication required. Please provide a valid Bearer token."}""");
        },
        OnForbidden = ctx =>
        {
            ctx.Response.StatusCode  = 403;
            ctx.Response.ContentType = "application/json";
            return ctx.Response.WriteAsync(
                """{"success":false,"message":"You do not have permission to perform this action."}""");
        },
    };
});

// ── 4. Authorization Policies ──────────────────────────
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin",    p => p.RequireClaim("roleName", "Admin"))
    .AddPolicy("RequireManager",  p => p.RequireClaim("roleName", "Admin", "ProjectManager"))
    .AddPolicy("RequireAnyRole",  p => p.RequireClaim("roleName", "Admin", "ProjectManager", "Collaborator"))
    .AddPolicy("CanCreateTasks",  p => p.RequireClaim("roleName", "Admin", "ProjectManager"))
    .AddPolicy("CanManageUsers",  p => p.RequireClaim("roleName", "Admin"))
    .AddPolicy("CanViewReports",  p => p.RequireClaim("roleName", "Admin", "ProjectManager"));

// ── 5. Rate Limiting ───────────────────────────────────
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("auth", cfg =>
    {
        cfg.PermitLimit = 5;
        cfg.Window      = TimeSpan.FromMinutes(1);
        cfg.QueueLimit  = 0;
    });
    opts.AddFixedWindowLimiter("passwordReset", cfg =>
    {
        cfg.PermitLimit = 3;
        cfg.Window      = TimeSpan.FromHours(1);
        cfg.QueueLimit  = 0;
    });
    opts.AddFixedWindowLimiter("general", cfg =>
    {
        cfg.PermitLimit = 100;
        cfg.Window      = TimeSpan.FromMinutes(1);
        cfg.QueueLimit  = 0;
    });
    opts.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.StatusCode  = 429;
        ctx.HttpContext.Response.ContentType = "application/json";
        await ctx.HttpContext.Response.WriteAsync(
            """{"success":false,"message":"Too many requests. Please slow down and try again."}""", token);
    };
});

// ── 6. CORS ────────────────────────────────────────────
var origins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries);

builder.Services.AddCors(opts =>
    opts.AddPolicy("TaskFlowCors", policy =>
        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));

// ── 7. Controllers + JSON ──────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ── 8. Swagger / OpenAPI ───────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "TaskFlow API",
        Version     = "v1",
        Description = "Complete Task Management System — ASP.NET Core 8 Web API",
        Contact     = new OpenApiContact { Name = "TaskFlow Dev Team", Email = "dev@taskflow.com" },
    });

    opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        Description  = "Enter your JWT access token from the /auth/login endpoint.",
    });

    opts.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = []
    });

    // Include XML comments from controllers
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) opts.IncludeXmlComments(xmlPath);
});

// ── 9. SignalR ─────────────────────────────────────────
builder.Services.AddSignalR(opts =>
{
    opts.EnableDetailedErrors = builder.Environment.IsDevelopment();
    opts.MaximumReceiveMessageSize = 64 * 1024; // 64 KB
});

// ── 10. Health Checks ──────────────────────────────────
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "sqlserver", tags: ["db", "sql"]);

// ════════════════════════════════════════════════════════
// BUILD
// ════════════════════════════════════════════════════════
var app = builder.Build();

// Auto-apply EF Core migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskFlow.Infrastructure.Persistence.TaskFlowDbContext>();
    await db.Database.MigrateAsync();
}

// ════════════════════════════════════════════════════════
// MIDDLEWARE PIPELINE (ORDER MATTERS!)
// ════════════════════════════════════════════════════════

// 1. Global exception handler — MUST be first
app.UseMiddleware<ExceptionMiddleware>();

// 2. HTTPS redirect
app.UseHttpsRedirection();

// 3. Swagger UI (development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API v1");
        c.RoutePrefix          = string.Empty; // Serve at root "/"
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.DisplayRequestDuration();
    });
}

// 4. Security response headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Frame-Options",           "DENY");
    ctx.Response.Headers.Append("X-Content-Type-Options",    "nosniff");
    ctx.Response.Headers.Append("X-XSS-Protection",          "1; mode=block");
    ctx.Response.Headers.Append("Referrer-Policy",           "strict-origin-when-cross-origin");
    ctx.Response.Headers.Remove("Server");
    await next();
});

// 5. CORS
app.UseCors("TaskFlowCors");

// 6. Rate limiting
app.UseRateLimiter();

// 7. Serilog request logging
app.UseSerilogRequestLogging(opts =>
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} [{Elapsed:0}ms]");

// 8. Authentication → Authorization (ORDER MATTERS)
app.UseAuthentication();
app.UseAuthorization();

// 9. Request logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

// 10. Map routes
app.MapControllers();

// 11. SignalR hub
app.MapHub<NotificationHub>("/hubs/notifications");

// 12. Health check endpoint
app.MapHealthChecks("/health");

// 13. API info endpoint
app.MapGet("/api/v1/info", () => Results.Ok(new
{
    name      = "TaskFlow API",
    version   = "1.0.0",
    status    = "running",
    timestamp = DateTime.UtcNow,
})).AllowAnonymous();

app.Run();
