using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Interfaces.Repositories;
using TaskFlow.Application.Common.Models;
using TaskFlow.Application.DTOs.Auth;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Application.Services;

public interface IUserService
{
    Task<UserDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PagedResult<UserDto>> GetAllAsync(int page, int pageSize, string? search, bool? isActive, byte? roleId, CancellationToken ct = default);
    Task<UserDto> UpdateProfileAsync(long userId, UpdateProfileDto dto, CancellationToken ct = default);
    Task<UserDto> UpdateAvatarAsync(long userId, string avatarUrl, CancellationToken ct = default);
    Task ToggleStatusAsync(long userId, bool isActive, long adminId, CancellationToken ct = default);
    Task AssignRoleAsync(long userId, byte roleId, long adminId, CancellationToken ct = default);
    Task<UserStatsDto> GetStatsAsync(long userId, CancellationToken ct = default);
    Task<IEnumerable<WorkloadDto>> GetTeamWorkloadAsync(long projectId, CancellationToken ct = default);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _log;

    public UserService(IUserRepository userRepo, IAuditLogRepository auditRepo, IMapper mapper, ILogger<UserService> log)
    { _userRepo = userRepo; _auditRepo = auditRepo; _mapper = mapper; _log = log; }

    public async Task<UserDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(id, ct) ?? throw new NotFoundException("User", id);
        return _mapper.Map<UserDto>(user);
    }

    public async Task<PagedResult<UserDto>> GetAllAsync(int page, int pageSize, string? search, bool? isActive, byte? roleId, CancellationToken ct = default)
    {
        var result = await _userRepo.GetAllAsync(page, pageSize, search, isActive, roleId, ct);
        return new PagedResult<UserDto>
        {
            Items      = _mapper.Map<IEnumerable<UserDto>>(result.Items),
            TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize,
        };
    }

    public async Task<UserDto> UpdateProfileAsync(long userId, UpdateProfileDto dto, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User", userId);
        user.FullName   = dto.FullName.Trim();
        user.JobTitle   = dto.JobTitle?.Trim();
        user.Department = dto.Department?.Trim();
        user.Bio        = dto.Bio?.Trim();
        user.UpdatedAt  = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, ct);
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> UpdateAvatarAsync(long userId, string avatarUrl, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User", userId);
        user.AvatarUrl = avatarUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, ct);
        return _mapper.Map<UserDto>(user);
    }

    public async Task ToggleStatusAsync(long userId, bool isActive, long adminId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User", userId);
        user.IsActive  = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, ct);
        await _auditRepo.LogAsync(new Domain.Entities.AuditLog
        {
            UserId = adminId, Action = isActive ? "ACCOUNT_UNLOCKED" : "ACCOUNT_LOCKED",
            AdditionalData = $"{{\"targetUserId\": {userId}}}",
        }, ct);
        _log.LogInformation("User {Id} status set to {Active} by admin {AdminId}", userId, isActive, adminId);
    }

    public async Task AssignRoleAsync(long userId, byte roleId, long adminId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User", userId);
        var existing = user.UserRoles.FirstOrDefault();
        if (existing != null) user.UserRoles.Remove(existing);
        user.UserRoles.Add(new Domain.Entities.UserRole
        {
            UserId = userId, RoleId = roleId, AssignedAt = DateTime.UtcNow, AssignedBy = adminId,
        });
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, ct);
        await _auditRepo.LogAsync(new Domain.Entities.AuditLog
        {
            UserId = adminId, Action = AuditActions.RoleAssigned,
            AdditionalData = $"{{\"targetUserId\": {userId}, \"roleId\": {roleId}}}",
        }, ct);
    }

    public async Task<UserStatsDto> GetStatsAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User", userId);
        var tasks = user.TaskAssignments.Select(a => a.Task).ToList();
        var total     = tasks.Count;
        var completed = tasks.Count(t => t.Status == Domain.Enums.TaskStatus.Done);
        return new UserStatsDto
        {
            TotalAssignedTasks = total, CompletedTasks = completed,
            InProgressTasks    = tasks.Count(t => t.Status == Domain.Enums.TaskStatus.InProgress),
            OverdueTasks       = tasks.Count(t => t.DueDate < DateOnly.FromDateTime(DateTime.UtcNow) && t.Status != Domain.Enums.TaskStatus.Done),
            CompletionRate     = total == 0 ? 0 : Math.Round((double)completed / total * 100, 1),
            TotalProjects      = user.ProjectMemberships.Count,
            TotalComments      = user.Comments.Count(c => !c.IsDeleted),
        };
    }

    public async Task<IEnumerable<WorkloadDto>> GetTeamWorkloadAsync(long projectId, CancellationToken ct = default)
    {
        var members = await _userRepo.GetProjectMembersAsync(projectId, ct);
        return members.Select(u =>
        {
            var tasks = u.TaskAssignments.Select(a => a.Task).Where(t => t.ProjectId == projectId && !t.IsArchived).ToList();
            return new WorkloadDto
            {
                UserId = u.Id, FullName = u.FullName, AvatarUrl = u.AvatarUrl,
                TotalTasks = tasks.Count,
                TodoTasks  = tasks.Count(t => t.Status == Domain.Enums.TaskStatus.Todo),
                InProgressTasks = tasks.Count(t => t.Status == Domain.Enums.TaskStatus.InProgress),
                DoneTasks  = tasks.Count(t => t.Status == Domain.Enums.TaskStatus.Done),
                OverdueTasks = tasks.Count(t => t.DueDate < DateOnly.FromDateTime(DateTime.UtcNow) && t.Status != Domain.Enums.TaskStatus.Done),
            };
        });
    }
}
