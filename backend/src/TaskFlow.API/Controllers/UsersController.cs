using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs.Auth;
using TaskFlow.Application.Services;

namespace TaskFlow.API.Controllers;

/// <summary>User Management — CRUD, profile, role assignment, stats, workload</summary>
[Authorize]
[Tags("Users")]
public class UsersController : BaseApiController
{
    private readonly IUserService _userSvc;
    public UsersController(IUserService userSvc) => _userSvc = userSvc;

    /// <summary>Get all users with optional filters. Admin only.</summary>
    [HttpGet]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] bool? isActive = null,
        [FromQuery] byte? roleId = null, CancellationToken ct = default)
    {
        var result = await _userSvc.GetAllAsync(page, pageSize, search, isActive, roleId, ct);
        return OkResponse(result);
    }

    /// <summary>Get a user by ID. Admin only.</summary>
    [HttpGet("{id:long}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var user = await _userSvc.GetByIdAsync(id, ct);
        return OkResponse(user);
    }

    /// <summary>Get the current user's own profile.</summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var user = await _userSvc.GetByIdAsync(CurrentUserId!.Value, ct);
        return OkResponse(user);
    }

    /// <summary>Update the current user's profile.</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken ct)
    {
        var user = await _userSvc.UpdateProfileAsync(CurrentUserId!.Value, dto, ct);
        return OkResponse(user, "Profile updated.");
    }

    /// <summary>Update the current user's avatar URL.</summary>
    [HttpPatch("profile/avatar")]
    public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarDto dto, CancellationToken ct)
    {
        var user = await _userSvc.UpdateAvatarAsync(CurrentUserId!.Value, dto.AvatarUrl, ct);
        return OkResponse(user, "Avatar updated.");
    }

    /// <summary>Activate or deactivate a user account. Admin only.</summary>
    [HttpPatch("{id:long}/status")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> ToggleStatus(long id, [FromBody] ToggleUserStatusDto dto, CancellationToken ct)
    {
        await _userSvc.ToggleStatusAsync(id, dto.IsActive, CurrentUserId!.Value, ct);
        return OkNoData($"User {(dto.IsActive ? "activated" : "deactivated")} successfully.");
    }

    /// <summary>Assign a system role to a user. Admin only.</summary>
    [HttpPatch("{id:long}/role")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> AssignRole(long id, [FromBody] AssignRoleDto dto, CancellationToken ct)
    {
        await _userSvc.AssignRoleAsync(id, dto.RoleId, CurrentUserId!.Value, ct);
        return OkNoData("Role assigned successfully.");
    }

    /// <summary>Get activity stats for a user.</summary>
    [HttpGet("{id:long}/stats")]
    public async Task<IActionResult> GetStats(long id, CancellationToken ct)
    {
        var stats = await _userSvc.GetStatsAsync(id, ct);
        return OkResponse(stats);
    }

    /// <summary>Get team workload for a project.</summary>
    [HttpGet("workload")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> GetWorkload([FromQuery] long projectId, CancellationToken ct)
    {
        var workload = await _userSvc.GetTeamWorkloadAsync(projectId, ct);
        return OkResponse(workload);
    }
}
