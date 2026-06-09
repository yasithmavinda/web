namespace TaskFlow.Application.Common.Interfaces;

/// <summary>
/// INotificationHubService — The bridge between your business logic and SignalR.
///
/// Beginner: Your services (TaskService, CommentService, etc.) should NOT
/// know about SignalR directly. Instead, they call THIS interface.
/// The Infrastructure layer implements it using IHubContext&lt;NotificationHub&gt;.
///
/// This keeps Clean Architecture clean:
///   Application layer  → only knows INotificationHubService (interface)
///   Infrastructure layer → knows SignalR (implementation detail)
///
/// ┌─────────────────────────────────────────────────────────┐
/// │  TaskService.CreateTask()                               │
/// │      → await _hubService.NotifyTaskCreated(...)         │
/// │                │                                        │
/// │                ▼                                        │
/// │  NotificationHubService (Infrastructure)                │
/// │      → await _hub.Clients.Group("project-5")           │
/// │              .SendAsync("TaskCreated", task)            │
/// └─────────────────────────────────────────────────────────┘
/// </summary>
public interface INotificationHubService
{
    // ── Task Events ──────────────────────────────────────────────

    /// <summary>Broadcast a new task to everyone in the project's SignalR group.</summary>
    Task NotifyTaskCreatedAsync(long projectId, object taskPayload);

    /// <summary>Broadcast updated task details to everyone in the project group.</summary>
    Task NotifyTaskUpdatedAsync(long projectId, object taskPayload);

    /// <summary>
    /// Notify when a task's status changes (Kanban card moves columns).
    /// Sent to the project group so all boards update in real-time.
    /// </summary>
    Task NotifyTaskStatusChangedAsync(long projectId, object taskPayload);

    /// <summary>
    /// Notify assigned users when they are assigned to a task.
    /// Sent to each assigned user's personal group.
    /// </summary>
    Task NotifyTaskAssignedAsync(IEnumerable<long> assignedUserIds, object taskPayload);

    /// <summary>Notify when a task position changes (drag-and-drop reorder).</summary>
    Task NotifyTaskPositionChangedAsync(long projectId, object payload);

    // ── Comment Events ───────────────────────────────────────────

    /// <summary>Broadcast a new comment to everyone viewing the task.</summary>
    Task NotifyCommentAddedAsync(long taskId, long projectId, object commentPayload);

    /// <summary>Broadcast comment edit to task viewers.</summary>
    Task NotifyCommentUpdatedAsync(long taskId, object commentPayload);

    /// <summary>Broadcast comment deletion to task viewers.</summary>
    Task NotifyCommentDeletedAsync(long taskId, long commentId);

    // ── Project Events ────────────────────────────────────────────

    /// <summary>Notify project members when a new member joins.</summary>
    Task NotifyProjectMemberAddedAsync(long projectId, object memberPayload);

    /// <summary>Notify project members when a member is removed.</summary>
    Task NotifyProjectMemberRemovedAsync(long projectId, long removedUserId);

    // ── Personal Notifications ────────────────────────────────────

    /// <summary>
    /// Send a personal notification to a SPECIFIC user.
    /// If the user is offline, this is skipped (notification already stored in DB).
    /// When user reconnects, they load unread notifications from the DB.
    /// </summary>
    Task SendNotificationToUserAsync(long userId, object notificationPayload);

    /// <summary>Send the same notification to multiple users at once.</summary>
    Task SendNotificationToUsersAsync(IEnumerable<long> userIds, object notificationPayload);

    /// <summary>
    /// Update the unread notification count badge for a user.
    /// Called after creating or marking notifications as read.
    /// </summary>
    Task UpdateUnreadCountAsync(long userId, int unreadCount);

    // ── Presence Queries ─────────────────────────────────────────

    /// <summary>Returns true if the user currently has an active WebSocket connection.</summary>
    bool IsUserOnline(long userId);

    /// <summary>Returns all currently online user IDs.</summary>
    IReadOnlyList<string> GetOnlineUserIds();
}
