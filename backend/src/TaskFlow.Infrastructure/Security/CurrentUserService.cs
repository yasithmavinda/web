using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Infrastructure.Security;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;
    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? User => _http.HttpContext?.User;

    public long? UserId => long.TryParse(
        User?.FindFirstValue("uid") ?? User?.FindFirstValue(ClaimTypes.NameIdentifier),
        out var id) ? id : null;

    public string? Email     => User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue("email");
    public string? RoleName  => User?.FindFirstValue("roleName");
    public byte?   RoleId    => byte.TryParse(User?.FindFirstValue("roleId"), out var r) ? r : null;
    public bool    IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public bool    IsAdmin         => RoleName == "Admin";
    public bool    IsProjectManager => RoleName is "Admin" or "ProjectManager";
}
