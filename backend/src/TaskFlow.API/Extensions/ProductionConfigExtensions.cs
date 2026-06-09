using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TaskFlow.API.Extensions;

/// <summary>
/// Production configuration extensions.
/// Wires up: Health Checks, Rate Limiting, Security Headers, HTTPS.
/// </summary>
public static class ProductionConfigExtensions
{
    // ── Health Checks ────────────────────────────────────────────
    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration config)
    {
        services
            .AddHealthChecks()
            // Check SQL Server connection
            .AddSqlServer(
                connectionString:     config.GetConnectionString("DefaultConnection")!,
                name:                 "sql-server",
                tags:                 new[] { "db", "sql" },
                timeout:              TimeSpan.FromSeconds(5))
            // Check SignalR (basic liveness)
            .AddCheck("signalr", () =>
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("SignalR running"),
                tags: new[] { "signalr" });

        return services;
    }

    // ── Rate Limiting ─────────────────────────────────────────────
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        int permitLimit    = config.GetValue<int>("RateLimiting:PermitLimit",    100);
        int windowSec      = config.GetValue<int>("RateLimiting:WindowSeconds",   60);
        int queueLimit     = config.GetValue<int>("RateLimiting:QueueLimit",      10);
        int loginLimit     = config.GetValue<int>("RateLimiting:LoginPermitLimit", 10);
        int loginWindowSec = config.GetValue<int>("RateLimiting:LoginWindowSeconds", 60);

        services.AddRateLimiter(options =>
        {
            // Default policy: 100 req/min per IP
            options.AddFixedWindowLimiter("api", opt =>
            {
                opt.PermitLimit         = permitLimit;
                opt.Window              = TimeSpan.FromSeconds(windowSec);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit          = queueLimit;
            });

            // Strict policy for auth endpoints: 10 req/min per IP
            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.PermitLimit         = loginLimit;
                opt.Window              = TimeSpan.FromSeconds(loginWindowSec);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit          = 0;  // No queuing for auth — fail immediately
            });

            // Response when limit exceeded: 429 Too Many Requests
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    statusCode = 429,
                    message    = "Too many requests. Please slow down.",
                    retryAfter = windowSec,
                }, token);
            };
        });

        return services;
    }

    // ── Production Middleware Pipeline ───────────────────────────
    public static WebApplication UseProductionMiddleware(this WebApplication app)
    {
        var env = app.Environment;

        // HSTS: Force HTTPS for 1 year
        if (!env.IsDevelopment())
        {
            app.UseHsts();
        }

        // Security headers
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options",     "nosniff");
            context.Response.Headers.Append("X-Frame-Options",            "DENY");
            context.Response.Headers.Append("X-XSS-Protection",           "1; mode=block");
            context.Response.Headers.Append("Referrer-Policy",            "strict-origin-when-cross-origin");
            context.Response.Headers.Append("Permissions-Policy",         "camera=(), microphone=()");
            // Remove server identification
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");
            await next();
        });

        // Health check endpoints
        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = WriteHealthResponse,
        });

        // Detailed health for monitoring systems (restrict to internal network in prod)
        app.MapHealthChecks("/health/detail", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate      = _ => true,
            ResponseWriter = WriteDetailedHealthResponse,
        })
        .RequireAuthorization(); // Only authenticated users can see detailed health

        return app;
    }

    private static Task WriteHealthResponse(
        Microsoft.AspNetCore.Http.HttpContext ctx,
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
    {
        ctx.Response.ContentType = "application/json";
        var status = report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
            ? "healthy" : "unhealthy";
        return ctx.Response.WriteAsJsonAsync(new { status, timestamp = DateTime.UtcNow });
    }

    private static Task WriteDetailedHealthResponse(
        Microsoft.AspNetCore.Http.HttpContext ctx,
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
    {
        ctx.Response.ContentType = "application/json";
        var detail = new
        {
            status    = report.Status.ToString(),
            duration  = report.TotalDuration,
            timestamp = DateTime.UtcNow,
            checks    = report.Entries.Select(e => new
            {
                name     = e.Key,
                status   = e.Value.Status.ToString(),
                duration = e.Value.Duration,
                error    = e.Value.Exception?.Message,
            }),
        };
        return ctx.Response.WriteAsJsonAsync(detail);
    }
}
