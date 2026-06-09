using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Common.Models;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    // ── Current User from JWT Claims ─────────────────────────
    protected long? CurrentUserId => long.TryParse(
        User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
        out var id) ? id : null;

    protected string? CurrentUserEmail  => User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(JwtRegisteredClaimNames.Email);
    protected string? CurrentRoleName   => User.FindFirstValue("roleName");
    protected byte?   CurrentRoleId     => byte.TryParse(User.FindFirstValue("roleId"), out var r) ? r : null;
    protected bool    IsAdmin           => CurrentRoleName == "Admin";
    protected bool    IsProjectManager  => CurrentRoleName is "Admin" or "ProjectManager";
    protected bool    IsCollaborator    => CurrentRoleName is "Admin" or "ProjectManager" or "Collaborator";

    // ── Response Helpers ──────────────────────────────────────
    protected IActionResult OkResponse<T>(T data, string message = "Success")
        => Ok(ApiResponse<T>.Ok(data, message));

    protected IActionResult CreatedResponse<T>(T data, string message = "Created successfully")
        => StatusCode(201, ApiResponse<T>.Created(data, message));

    protected IActionResult OkNoData(string message = "Success")
        => Ok(ApiResponse.OkNoData(message));

    protected IActionResult NotFound(string message)
        => base.NotFound(ApiResponse.Fail(message));

    protected IActionResult Forbidden(string message)
        => StatusCode(403, ApiResponse.Fail(message));

    protected string GetClientIp()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded)) return forwarded.Split(',')[0].Trim();
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
