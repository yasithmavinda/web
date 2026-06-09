using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces.Repositories;
using TaskFlow.Application.Common.Models;
using TaskFlow.Application.DTOs.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;
using TaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Infrastructure.Persistence.Repositories;

// ── User Repository ──────────────────────────────────────────────
public class UserRepository : IUserRepository
{
    private readonly TaskFlowDbContext _db;
    public UserRepository(TaskFlowDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(long id, CancellationToken ct = default)
        => await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.ProjectMemberships)
            .Include(u => u.TaskAssignments).ThenInclude(a => a.Task)
            .Include(u => u.Comments)
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null, ct);

    public async Task<User?> GetByEmailVerifyTokenAsync(string token, CancellationToken ct = default)
        => await _db.Users
            .FirstOrDefaultAsync(u => u.EmailVerifyToken == token && u.DeletedAt == null, ct);

    public async Task<PagedResult<User>> GetAllAsync(int page, int pageSize, string? search,
        bool? isActive, byte? roleId, CancellationToken ct = default)
    {
        var query = _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));
        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);
        if (roleId.HasValue)
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId.Value));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<User> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<User> CreateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => await _db.Users.AnyAsync(u => u.Email == email && u.DeletedAt == null, ct);

    public async Task<IEnumerable<User>> GetProjectMembersAsync(long projectId, CancellationToken ct = default)
        => await _db.Users
            .Include(u => u.TaskAssignments).ThenInclude(a => a.Task)
            .Where(u => u.ProjectMemberships.Any(m => m.ProjectId == projectId))
            .ToListAsync(ct);
}

// ── Refresh Token Repository ─────────────────────────────────────
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly TaskFlowDbContext _db;
    public RefreshTokenRepository(TaskFlowDbContext db) => _db = db;

    public async Task<RefreshToken?> GetByHashAsync(byte[] hash, CancellationToken ct = default)
        => await _db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.UserRoles)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

    public async Task<IEnumerable<RefreshToken>> GetActiveForUserAsync(long userId, CancellationToken ct = default)
        => await _db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<int> CountActiveTokensAsync(long userId, CancellationToken ct = default)
        => await _db.RefreshTokens.CountAsync(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow, ct);

    public async Task CreateAsync(RefreshToken token, CancellationToken ct = default)
    {
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        _db.RefreshTokens.Update(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAllForUserAsync(long userId, string reason, CancellationToken ct = default)
    {
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsRevoked, true)
                .SetProperty(t => t.RevokedAt, DateTime.UtcNow)
                .SetProperty(t => t.RevokedReason, reason), ct);
    }

    public async Task RevokeOldestForUserAsync(long userId, string reason, CancellationToken ct = default)
    {
        var oldest = await _db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (oldest != null) { oldest.Revoke(reason); await _db.SaveChangesAsync(ct); }
    }
}

// ── Password Reset Token Repository ─────────────────────────────
public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly TaskFlowDbContext _db;
    public PasswordResetTokenRepository(TaskFlowDbContext db) => _db = db;

    public async Task<PasswordResetToken?> GetByHashAsync(byte[] hash, CancellationToken ct = default)
        => await _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

    public async Task CreateAsync(PasswordResetToken token, CancellationToken ct = default)
    { _db.PasswordResetTokens.Add(token); await _db.SaveChangesAsync(ct); }

    public async Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default)
    { _db.PasswordResetTokens.Update(token); await _db.SaveChangesAsync(ct); }

    public async Task InvalidateAllForUserAsync(long userId, CancellationToken ct = default)
        => await _db.PasswordResetTokens
            .Where(t => t.UserId == userId && !t.IsUsed)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsUsed, true).SetProperty(t => t.UsedAt, DateTime.UtcNow), ct);
}

// ── Audit Log Repository ─────────────────────────────────────────
public class AuditLogRepository : IAuditLogRepository
{
    private readonly TaskFlowDbContext _db;
    public AuditLogRepository(TaskFlowDbContext db) => _db = db;

    public async Task LogAsync(AuditLog log, CancellationToken ct = default)
    { _db.AuditLogs.Add(log); await _db.SaveChangesAsync(ct); }
}

// ── Project Repository ───────────────────────────────────────────
public class ProjectRepository : IProjectRepository
{
    private readonly TaskFlowDbContext _db;
    public ProjectRepository(TaskFlowDbContext db) => _db = db;

    public async Task<Project?> GetByIdAsync(long id, CancellationToken ct = default)
        => await _db.Projects
            .Include(p => p.Owner)
            .Include(p => p.Tasks.Where(t => !t.IsArchived))
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsArchived, ct);

    public async Task<Project?> GetByIdWithMembersAsync(long id, CancellationToken ct = default)
        => await _db.Projects
            .Include(p => p.Owner)
            .Include(p => p.Members).ThenInclude(m => m.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(p => p.Tasks.Where(t => !t.IsArchived))
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsArchived, ct);

    public async Task<PagedResult<Project>> GetAllAsync(long? userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Projects
            .Include(p => p.Owner)
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .Where(p => !p.IsArchived);

        if (userId.HasValue)
            query = query.Where(p => p.Members.Any(m => m.UserId == userId.Value) || p.OwnerId == userId.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Project> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<Project> CreateAsync(Project project, CancellationToken ct = default)
    { _db.Projects.Add(project); await _db.SaveChangesAsync(ct); return project; }

    public async Task UpdateAsync(Project project, CancellationToken ct = default)
    { _db.Projects.Update(project); await _db.SaveChangesAsync(ct); }

    public async Task<bool> IsUserMemberAsync(long projectId, long userId, CancellationToken ct = default)
        => await _db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == userId, ct)
        || await _db.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == userId, ct);

    public async Task AddMemberAsync(ProjectMember member, CancellationToken ct = default)
    { _db.ProjectMembers.Add(member); await _db.SaveChangesAsync(ct); }

    public async Task RemoveMemberAsync(long projectId, long userId, CancellationToken ct = default)
    {
        var m = await _db.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId, ct);
        if (m != null) { _db.ProjectMembers.Remove(m); await _db.SaveChangesAsync(ct); }
    }
}

// ── Task Repository ──────────────────────────────────────────────
public class TaskRepository : ITaskRepository
{
    private readonly TaskFlowDbContext _db;
    public TaskRepository(TaskFlowDbContext db) => _db = db;

    public async Task<TaskItem?> GetByIdAsync(long id, CancellationToken ct = default)
        => await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<TaskItem?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default)
        => await _db.Tasks
            .Include(t => t.Project)
            .Include(t => t.CreatedByUser)
            .Include(t => t.Assignments).ThenInclude(a => a.AssignedTo).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(t => t.TaskTags).ThenInclude(tt => tt.Tag)
            .Include(t => t.SubTasks)
            .Include(t => t.Comments.Where(c => !c.IsDeleted))
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResult<TaskItem>> GetFilteredAsync(TaskFilterDto f, CancellationToken ct = default)
    {
        var q = _db.Tasks
            .Include(t => t.Project).Include(t => t.CreatedByUser)
            .Include(t => t.Assignments).ThenInclude(a => a.AssignedTo).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(t => t.TaskTags).ThenInclude(tt => tt.Tag)
            .Include(t => t.SubTasks).Include(t => t.Comments).Include(t => t.Attachments)
            .Where(t => !t.IsArchived);

        // Restrict to user's projects
        q = q.Where(t => t.Project.Members.Any(m => m.UserId == f.RequestingUserId) || t.Project.OwnerId == f.RequestingUserId);

        if (f.ProjectId.HasValue) q = q.Where(t => t.ProjectId == f.ProjectId.Value);
        if (!string.IsNullOrEmpty(f.Status) && Enum.TryParse<TaskStatus>(f.Status, out var st)) q = q.Where(t => t.Status == st);
        if (!string.IsNullOrEmpty(f.Priority) && Enum.TryParse<Domain.Enums.TaskPriority>(f.Priority, out var pri)) q = q.Where(t => t.Priority == pri);
        if (f.AssigneeId.HasValue) q = q.Where(t => t.Assignments.Any(a => a.AssignedToUserId == f.AssigneeId.Value));
        if (!string.IsNullOrEmpty(f.Search)) q = q.Where(t => t.Title.Contains(f.Search) || (t.Description != null && t.Description.Contains(f.Search)));
        if (f.IsOverdue == true) q = q.Where(t => t.DueDate < DateOnly.FromDateTime(DateTime.UtcNow) && t.Status != TaskStatus.Done);
        if (f.ParentTaskId.HasValue) q = q.Where(t => t.ParentTaskId == f.ParentTaskId.Value);

        q = f.SortBy switch
        {
            "DueDate"  => f.SortOrder == "ASC" ? q.OrderBy(t => t.DueDate)  : q.OrderByDescending(t => t.DueDate),
            "Priority" => f.SortOrder == "ASC" ? q.OrderBy(t => t.Priority) : q.OrderByDescending(t => t.Priority),
            "Title"    => f.SortOrder == "ASC" ? q.OrderBy(t => t.Title)    : q.OrderByDescending(t => t.Title),
            "Position" => q.OrderBy(t => t.Position),
            _          => f.SortOrder == "ASC" ? q.OrderBy(t => t.CreatedAt): q.OrderByDescending(t => t.CreatedAt),
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((f.Page - 1) * f.PageSize).Take(f.PageSize).ToListAsync(ct);
        return new PagedResult<TaskItem> { Items = items, TotalCount = total, Page = f.Page, PageSize = f.PageSize };
    }

    public async Task<IEnumerable<TaskItem>> GetSubTasksAsync(long parentId, CancellationToken ct = default)
        => await _db.Tasks.Where(t => t.ParentTaskId == parentId && !t.IsArchived).ToListAsync(ct);

    public async Task<IEnumerable<TaskStatusHistory>> GetStatusHistoryAsync(long taskId, CancellationToken ct = default)
        => await _db.TaskStatusHistories
            .Include(h => h.ChangedByUser)
            .Where(h => h.TaskId == taskId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(ct);

    public async Task<TaskItem> CreateAsync(TaskItem task, CancellationToken ct = default)
    { _db.Tasks.Add(task); await _db.SaveChangesAsync(ct); return task; }

    public async Task UpdateAsync(TaskItem task, CancellationToken ct = default)
    { task.UpdatedAt = DateTime.UtcNow; _db.Tasks.Update(task); await _db.SaveChangesAsync(ct); }

    public async Task AddAssignmentAsync(TaskAssignment assignment, CancellationToken ct = default)
    { _db.TaskAssignments.Add(assignment); await _db.SaveChangesAsync(ct); }

    public async Task RemoveAssignmentAsync(long taskId, long userId, CancellationToken ct = default)
    {
        var a = await _db.TaskAssignments.FirstOrDefaultAsync(a => a.TaskId == taskId && a.AssignedToUserId == userId, ct);
        if (a != null) { _db.TaskAssignments.Remove(a); await _db.SaveChangesAsync(ct); }
    }

    public async Task AddStatusHistoryAsync(TaskStatusHistory history, CancellationToken ct = default)
    { _db.TaskStatusHistories.Add(history); await _db.SaveChangesAsync(ct); }
}

// ── Comment Repository ───────────────────────────────────────────
public class CommentRepository : ICommentRepository
{
    private readonly TaskFlowDbContext _db;
    public CommentRepository(TaskFlowDbContext db) => _db = db;

    public async Task<Comment?> GetByIdAsync(long id, CancellationToken ct = default)
        => await _db.Comments.Include(c => c.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<PagedResult<Comment>> GetByTaskAsync(long taskId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Comments
            .Include(c => c.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(c => c.Replies).ThenInclude(r => r.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(c => c.TaskId == taskId && c.ParentCommentId == null)
            .OrderBy(c => c.CreatedAt);
        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Comment> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<Comment> CreateAsync(Comment comment, CancellationToken ct = default)
    { _db.Comments.Add(comment); await _db.SaveChangesAsync(ct); return comment; }

    public async Task UpdateAsync(Comment comment, CancellationToken ct = default)
    { comment.UpdatedAt = DateTime.UtcNow; _db.Comments.Update(comment); await _db.SaveChangesAsync(ct); }
}

// ── Notification Repository ──────────────────────────────────────
public class NotificationRepository : INotificationRepository
{
    private readonly TaskFlowDbContext _db;
    public NotificationRepository(TaskFlowDbContext db) => _db = db;

    public async Task<PagedResult<Notification>> GetForUserAsync(long userId, bool? isRead, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Notifications
            .Include(n => n.Actor).ThenInclude(a => a!.UserRoles).ThenInclude(ur => ur.Role)
            .Where(n => n.RecipientId == userId);
        if (isRead.HasValue) q = q.Where(n => n.IsRead == isRead.Value);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(n => n.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Notification> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default)
        => await _db.Notifications.CountAsync(n => n.RecipientId == userId && !n.IsRead, ct);

    public async Task<Notification?> GetByIdAsync(long id, CancellationToken ct = default)
        => await _db.Notifications.FirstOrDefaultAsync(n => n.NotificationId == id, ct);

    public async Task CreateAsync(Notification notification, CancellationToken ct = default)
    { _db.Notifications.Add(notification); await _db.SaveChangesAsync(ct); }

    public async Task UpdateAsync(Notification notification, CancellationToken ct = default)
    { _db.Notifications.Update(notification); await _db.SaveChangesAsync(ct); }

    public async Task MarkAllReadAsync(long userId, CancellationToken ct = default)
        => await _db.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true).SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
}
