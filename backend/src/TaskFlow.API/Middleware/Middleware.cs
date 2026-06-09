using System.Net;
using System.Text.Json;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.API.Middleware;

/// <summary>
/// Global exception handler — every unhandled exception lands here.
/// Converts domain exceptions → clean JSON responses. Never leaks stack traces in production.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _log;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log, IHostEnvironment env)
    { _next = next; _log = log; _env = env; }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex) { await HandleAsync(ctx, ex); }
    }

    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        _log.LogError(ex, "Exception [{Type}] | {Path} | User:{UserId} | IP:{IP}",
            ex.GetType().Name, ctx.Request.Path,
            ctx.User?.FindFirst("uid")?.Value ?? "anon",
            ctx.Connection.RemoteIpAddress?.ToString());

        var (code, message, errors) = ex switch
        {
            NotFoundException       e => (404, e.Message,         (object?)null),
            UnauthorizedException   e => (403, e.Message,         null),
            ConflictException       e => (409, e.Message,         null),
            BadRequestException     e => (400, e.Message,         null),
            AccountLockedException  e => (423, e.Message,         new { lockedUntil = e.LockedUntil }),
            ValidationException     e => (422, "Validation failed.", (object?)e.Errors),
            FluentValidation.ValidationException fv => (422,
                "One or more validation errors occurred.",
                fv.Errors.GroupBy(x => x.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray())),
            _ => (500,
                _env.IsDevelopment() ? $"[{ex.GetType().Name}] {ex.Message}" : "An unexpected error occurred.",
                (object?)(_env.IsDevelopment() ? ex.StackTrace : null))
        };

        ctx.Response.StatusCode  = code;
        ctx.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(
            new { success = false, message, errors, timestamp = DateTime.UtcNow.ToString("O") },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await ctx.Response.WriteAsync(body);
    }
}

/// <summary>Logs every HTTP request with method, path, status code, timing, IP, and user ID.</summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _log;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> log)
    { _next = next; _log = log; }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var start = DateTime.UtcNow;
        await _next(ctx);
        var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
        _log.LogInformation(
            "{Method} {Path} → {Status} in {Ms:0}ms | IP:{IP} | User:{UserId}",
            ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode, elapsed,
            ctx.Connection.RemoteIpAddress?.ToString(),
            ctx.User?.FindFirst("uid")?.Value ?? "anon");
    }
}
