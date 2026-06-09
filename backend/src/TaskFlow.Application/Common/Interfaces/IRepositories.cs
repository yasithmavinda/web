using TaskFlow.Domain.Entities;
using TaskFlow.Application.Common.Models;
using TaskFlow.Application.DTOs.Auth;
using TaskFlow.Application.DTOs.Tasks;

namespace TaskFlow.Application.Common.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByEmailVerifyTokenAsync(string token, CancellationToken ct = default);
    Task<PagedResult<User>> GetAllAsync(int page, int pageSize, string? search, bool? isActive, byte? roleId, CancellationToken ct = default);
    Task<User> CreateAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<IEnumerable<User>> GetProjectMembersAsync(long projectId, CancellationToken ct = default);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(byte[] hash, CancellationToken ct = default);
    Task<IEnumerable<RefreshToken>> GetActiveForUserAsync(long userId, CancellationToken ct = default);
    Task<int> CountActiveTokensAsync(long userId, CancellationToken ct = default);
    Task CreateAsync(RefreshToken token, CancellationToken ct = default);
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeAllForUserAsync(long userId, string reason, CancellationToken ct = default);
    Task RevokeOldestForUserAsync(long userId, string reason, CancellationToken ct = default);
}

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByHashAsync(byte[] hash, CancellationToken ct = default);
    Task CreateAsync(PasswordResetToken token, CancellationToken ct = default);
    Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default);
    Task InvalidateAllForUserAsync(long userId, CancellationToken ct = default);
}

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Project?> GetByIdWithMembersAsync(long id, CancellationToken ct = default);
    Task<PagedResult<Project>> GetAllAsync(long? userId, int page, int pageSize, CancellationToken ct = default);
    Task<Project> CreateAsync(Project project, CancellationToken ct = default);
    Task UpdateAsync(Project project, CancellationToken ct = default);
    Task<bool> IsUserMemberAsync(long projectId, long userId, CancellationToken ct = default);
    Task AddMemberAsync(ProjectMember member, CancellationToken ct = default);
    Task RemoveMemberAsync(long projectId, long userId, CancellationToken ct = default);
}

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<TaskItem?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default);
    Task<PagedResult<TaskItem>> GetFilteredAsync(TaskFilterDto filter, CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetSubTasksAsync(long parentTaskId, CancellationToken ct = default);
    Task<IEnumerable<TaskStatusHistory>> GetStatusHistoryAsync(long taskId, CancellationToken ct = default);
    Task<TaskItem> CreateAsync(TaskItem task, CancellationToken ct = default);
    Task UpdateAsync(TaskItem task, CancellationToken ct = default);
    Task AddAssignmentAsync(TaskAssignment assignment, CancellationToken ct = default);
    Task RemoveAssignmentAsync(long taskId, long userId, CancellationToken ct = default);
    Task AddStatusHistoryAsync(TaskStatusHistory history, CancellationToken ct = default);
}

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PagedResult<Comment>> GetByTaskAsync(long taskId, int page, int pageSize, CancellationToken ct = default);
    Task<Comment> CreateAsync(Comment comment, CancellationToken ct = default);
    Task UpdateAsync(Comment comment, CancellationToken ct = default);
}

public interface INotificationRepository
{
    Task<PagedResult<Notification>> GetForUserAsync(long userId, bool? isRead, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default);
    Task<Notification?> GetByIdAsync(long id, CancellationToken ct = default);
    Task CreateAsync(Notification notification, CancellationToken ct = default);
    Task UpdateAsync(Notification notification, CancellationToken ct = default);
    Task MarkAllReadAsync(long userId, CancellationToken ct = default);
}

public interface IAuditLogRepository
{
    Task LogAsync(AuditLog log, CancellationToken ct = default);
}
