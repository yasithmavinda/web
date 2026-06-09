using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TaskFlow.API.Hubs;
using TaskFlow.Application.DTOs.Comments;
using TaskFlow.Application.DTOs.Notifications;
using TaskFlow.Application.Services;

namespace TaskFlow.API.Controllers;

/// <summary>Comments — Threaded comments on tasks</summary>
[Authorize]
[Tags("Comments")]
public class CommentsController : BaseApiController
{
    private readonly ICommentService _commentSvc;
    private readonly IHubContext<NotificationHub> _hub;

    public CommentsController(ICommentService commentSvc, IHubContext<NotificationHub> hub)
    { _commentSvc = commentSvc; _hub = hub; }

    /// <summary>Get comments for a task (paginated, top-level only; replies are nested).</summary>
    [HttpGet("task/{taskId:long}")]
    public async Task<IActionResult> GetByTask(
        long taskId, [FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken ct = default)
    {
        var result = await _commentSvc.GetByTaskAsync(taskId, page, pageSize, CurrentUserId!.Value, ct);
        return OkResponse(result);
    }

    /// <summary>Add a comment to a task. Optionally reply to an existing comment.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCommentDto dto, CancellationToken ct)
    {
        var comment = await _commentSvc.CreateAsync(dto, CurrentUserId!.Value, ct);
        // Real-time notification
        await _hub.Clients.Group($"task-{dto.TaskId}").SendAsync("CommentAdded", comment, ct);
        return CreatedResponse(comment, "Comment added.");
    }

    /// <summary>Edit your own comment.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCommentDto dto, CancellationToken ct)
    {
        var comment = await _commentSvc.UpdateAsync(id, dto, CurrentUserId!.Value, ct);
        return OkResponse(comment, "Comment updated.");
    }

    /// <summary>Soft-delete a comment. Authors can delete their own; Admins can delete any.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _commentSvc.DeleteAsync(id, CurrentUserId!.Value, IsAdmin, ct);
        return OkNoData("Comment deleted.");
    }
}

/// <summary>Notifications — Inbox, unread count, mark read, settings</summary>
[Authorize]
[Tags("Notifications")]
[Route("api/v1/[controller]")]
[ApiController]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notifSvc;
    public NotificationsController(INotificationService notifSvc) => _notifSvc = notifSvc;

    /// <summary>Get notifications for the current user.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? isRead, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _notifSvc.GetForUserAsync(CurrentUserId!.Value, isRead, page, pageSize, ct);
        return OkResponse(result);
    }

    /// <summary>Get unread notification count (for the badge in the header).</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var count = await _notifSvc.GetUnreadCountAsync(CurrentUserId!.Value, ct);
        return OkResponse(new { unreadCount = count });
    }

    /// <summary>Mark a specific notification as read.</summary>
    [HttpPatch("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct)
    {
        await _notifSvc.MarkReadAsync(id, CurrentUserId!.Value, ct);
        return OkNoData("Notification marked as read.");
    }

    /// <summary>Mark ALL notifications as read.</summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _notifSvc.MarkAllReadAsync(CurrentUserId!.Value, ct);
        return OkNoData("All notifications marked as read.");
    }

    /// <summary>Get notification preferences.</summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        var settings = await _notifSvc.GetSettingsAsync(CurrentUserId!.Value, ct);
        return OkResponse(settings);
    }

    /// <summary>Update notification preferences.</summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] NotificationSettingDto dto, CancellationToken ct)
    {
        await _notifSvc.UpdateSettingsAsync(CurrentUserId!.Value, dto, ct);
        return OkNoData("Notification settings updated.");
    }
}
