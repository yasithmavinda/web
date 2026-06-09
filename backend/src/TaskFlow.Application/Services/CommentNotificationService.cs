using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Interfaces.Repositories;
using TaskFlow.Application.Common.Models;
using TaskFlow.Application.DTOs.Comments;
using TaskFlow.Application.DTOs.Notifications;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Application.Services;

// ── Comment Service ───────────────────────────────────────────────
public interface ICommentService
{
    Task<PagedResult<CommentDto>> GetByTaskAsync(long taskId, int page, int pageSize, long requestingUserId, CancellationToken ct = default);
    Task<CommentDto> CreateAsync(CreateCommentDto dto, long userId, CancellationToken ct = default);
    Task<CommentDto> UpdateAsync(long commentId, UpdateCommentDto dto, long userId, CancellationToken ct = default);
    Task DeleteAsync(long commentId, long userId, bool isAdmin, CancellationToken ct = default);
}

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepo;
    private readonly INotificationRepository _notifRepo;
    private readonly ITaskRepository _taskRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<CommentService> _log;

    public CommentService(ICommentRepository commentRepo, INotificationRepository notifRepo,
        ITaskRepository taskRepo, IMapper mapper, ILogger<CommentService> log)
    { _commentRepo = commentRepo; _notifRepo = notifRepo; _taskRepo = taskRepo; _mapper = mapper; _log = log; }

    public async Task<PagedResult<CommentDto>> GetByTaskAsync(long taskId, int page, int pageSize, long requestingUserId, CancellationToken ct = default)
    {
        var result = await _commentRepo.GetByTaskAsync(taskId, page, pageSize, ct);
        return new PagedResult<CommentDto>
        {
            Items = _mapper.Map<IEnumerable<CommentDto>>(result.Items),
            TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize,
        };
    }

    public async Task<CommentDto> CreateAsync(CreateCommentDto dto, long userId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(dto.TaskId, ct) ?? throw new NotFoundException("Task", dto.TaskId);

        var comment = new Comment
        {
            TaskId = dto.TaskId, UserId = userId,
            Body = dto.Body.Trim(), ParentCommentId = dto.ParentCommentId,
        };
        var created = await _commentRepo.CreateAsync(comment, ct);

        // Notify task creator
        if (task.CreatedBy != userId)
        {
            await _notifRepo.CreateAsync(new Notification
            {
                RecipientId = task.CreatedBy, ActorId = userId, TaskId = dto.TaskId,
                Type = NotificationType.CommentAdded,
                Title = "New Comment", Message = $"Someone commented on \"{task.Title}\".",
                ActionUrl = $"/tasks/{dto.TaskId}#comments",
            }, ct);
        }

        _log.LogInformation("Comment {Id} created on task {TaskId} by user {UserId}", created.Id, dto.TaskId, userId);
        return _mapper.Map<CommentDto>(created);
    }

    public async Task<CommentDto> UpdateAsync(long commentId, UpdateCommentDto dto, long userId, CancellationToken ct = default)
    {
        var comment = await _commentRepo.GetByIdAsync(commentId, ct) ?? throw new NotFoundException("Comment", commentId);
        if (comment.UserId != userId) throw new UnauthorizedException("You can only edit your own comments.");
        comment.Body = dto.Body.Trim(); comment.IsEdited = true; comment.UpdatedAt = DateTime.UtcNow;
        await _commentRepo.UpdateAsync(comment, ct);
        return _mapper.Map<CommentDto>(comment);
    }

    public async Task DeleteAsync(long commentId, long userId, bool isAdmin, CancellationToken ct = default)
    {
        var comment = await _commentRepo.GetByIdAsync(commentId, ct) ?? throw new NotFoundException("Comment", commentId);
        if (comment.UserId != userId && !isAdmin)
            throw new UnauthorizedException("You can only delete your own comments.");
        comment.IsDeleted = true; comment.Body = "[Comment deleted]"; comment.UpdatedAt = DateTime.UtcNow;
        await _commentRepo.UpdateAsync(comment, ct);
    }
}

// ── Notification Service ─────────────────────────────────────────
public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetForUserAsync(long userId, bool? isRead, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default);
    Task MarkReadAsync(long notificationId, long userId, CancellationToken ct = default);
    Task MarkAllReadAsync(long userId, CancellationToken ct = default);
    Task<NotificationSettingDto> GetSettingsAsync(long userId, CancellationToken ct = default);
    Task UpdateSettingsAsync(long userId, NotificationSettingDto dto, CancellationToken ct = default);
}

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifRepo;
    private readonly INotificationHubService _hub;
    private readonly IMapper _mapper;

    public NotificationService(INotificationRepository notifRepo, INotificationHubService hub, IMapper mapper)
    { _notifRepo = notifRepo; _hub = hub; _mapper = mapper; }

    public async Task<PagedResult<NotificationDto>> GetForUserAsync(long userId, bool? isRead, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _notifRepo.GetForUserAsync(userId, isRead, page, pageSize, ct);
        return new PagedResult<NotificationDto>
        {
            Items = _mapper.Map<IEnumerable<NotificationDto>>(result.Items),
            TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize,
        };
    }

    public async Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default)
        => await _notifRepo.GetUnreadCountAsync(userId, ct);

    public async Task MarkReadAsync(long notificationId, long userId, CancellationToken ct = default)
    {
        var notif = await _notifRepo.GetByIdAsync(notificationId, ct)
            ?? throw new NotFoundException("Notification", notificationId);
        if (notif.RecipientId != userId) throw new UnauthorizedException("Not your notification.");
        notif.IsRead = true; notif.ReadAt = DateTime.UtcNow;
        await _notifRepo.UpdateAsync(notif, ct);

        // Update badge in real-time
        int newCount = await _notifRepo.GetUnreadCountAsync(userId, ct);
        _ = Task.Run(() => _hub.UpdateUnreadCountAsync(userId, newCount));
    }

    public async Task MarkAllReadAsync(long userId, CancellationToken ct = default)
    {
        await _notifRepo.MarkAllReadAsync(userId, ct);
        _ = Task.Run(() => _hub.UpdateUnreadCountAsync(userId, 0));
    }

    public Task<NotificationSettingDto> GetSettingsAsync(long userId, CancellationToken ct = default)
        => Task.FromResult(new NotificationSettingDto
        {
            EmailOnTaskAssigned = true, EmailOnCommentAdded = true,
            EmailOnMentioned = true, EmailOnTaskOverdue = true,
            PushOnTaskAssigned = true, PushOnCommentAdded = true, InAppOnAll = true,
        });

    public Task UpdateSettingsAsync(long userId, NotificationSettingDto dto, CancellationToken ct = default)
        => Task.CompletedTask;
}
