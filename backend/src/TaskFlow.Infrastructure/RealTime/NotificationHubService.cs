using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Infrastructure.RealTime;

/// <summary>Generates SignalR group name strings — avoids circular dep on TaskFlow.API.Hubs.</summary>
internal static class HubGroups
{
    public static string Project(long id) => $"project-{id}";
    public static string Task(long id)    => $"task-{id}";
    public static string User(string id)  => $"user-{id}";
}

/// <summary>
/// NotificationHubService — Concrete implementation of INotificationHubService.
/// Uses IHubContext to send messages FROM the server to connected SignalR clients.
/// Uses a generic Hub marker so Infrastructure doesn't reference TaskFlow.API.
/// </summary>
public class NotificationHubService : INotificationHubService
{
    // We use the untyped IHubContext via a Hub marker interface.
    // The API layer registers the actual NotificationHub; we just need IHubContext.
    private readonly IHubContext<NotificationHubMarker> _hub;
    private readonly ILogger<NotificationHubService> _log;

    public NotificationHubService(
        IHubContext<NotificationHubMarker> hub,
        ILogger<NotificationHubService> log)
    { _hub = hub; _log = log; }

    // ── Task Events ──────────────────────────────────────────────

    public async Task NotifyTaskCreatedAsync(long projectId, object taskPayload)
        => await SafeSend("TaskCreated", _hub.Clients.Group(HubGroups.Project(projectId)), taskPayload, $"project-{projectId}");

    public async Task NotifyTaskUpdatedAsync(long projectId, object taskPayload)
        => await SafeSend("TaskUpdated", _hub.Clients.Group(HubGroups.Project(projectId)), taskPayload, $"project-{projectId}");

    public async Task NotifyTaskStatusChangedAsync(long projectId, object taskPayload)
        => await SafeSend("TaskStatusChanged", _hub.Clients.Group(HubGroups.Project(projectId)), taskPayload, $"project-{projectId}");

    public async Task NotifyTaskAssignedAsync(IEnumerable<long> assignedUserIds, object taskPayload)
    {
        var tasks = assignedUserIds.Select(uid =>
            SafeSend("TaskAssigned", _hub.Clients.Group(HubGroups.User(uid.ToString())), taskPayload, $"user-{uid}"));
        await Task.WhenAll(tasks);
    }

    public async Task NotifyTaskPositionChangedAsync(long projectId, object payload)
        => await SafeSend("TaskPositionChanged", _hub.Clients.Group(HubGroups.Project(projectId)), payload, $"project-{projectId}");

    // ── Comment Events ───────────────────────────────────────────

    public async Task NotifyCommentAddedAsync(long taskId, long projectId, object commentPayload)
    {
        await SafeSend("CommentAdded", _hub.Clients.Group(HubGroups.Task(taskId)), commentPayload, $"task-{taskId}");
        await SafeSend("ProjectActivityUpdated", _hub.Clients.Group(HubGroups.Project(projectId)), commentPayload, $"project-{projectId}");
    }

    public async Task NotifyCommentUpdatedAsync(long taskId, object commentPayload)
        => await SafeSend("CommentUpdated", _hub.Clients.Group(HubGroups.Task(taskId)), commentPayload, $"task-{taskId}");

    public async Task NotifyCommentDeletedAsync(long taskId, long commentId)
        => await SafeSend("CommentDeleted", _hub.Clients.Group(HubGroups.Task(taskId)), new { taskId, commentId }, $"task-{taskId}");

    // ── Project Events ────────────────────────────────────────────

    public async Task NotifyProjectMemberAddedAsync(long projectId, object memberPayload)
        => await SafeSend("ProjectMemberAdded", _hub.Clients.Group(HubGroups.Project(projectId)), memberPayload, $"project-{projectId}");

    public async Task NotifyProjectMemberRemovedAsync(long projectId, long removedUserId)
        => await SafeSend("ProjectMemberRemoved", _hub.Clients.Group(HubGroups.Project(projectId)), new { projectId, removedUserId }, $"project-{projectId}");

    // ── Personal Notifications ────────────────────────────────────

    public async Task SendNotificationToUserAsync(long userId, object notificationPayload)
        => await SafeSend("NewNotification", _hub.Clients.Group(HubGroups.User(userId.ToString())), notificationPayload, $"user-{userId}");

    public async Task SendNotificationToUsersAsync(IEnumerable<long> userIds, object notificationPayload)
    {
        var tasks = userIds.Select(id => SendNotificationToUserAsync(id, notificationPayload));
        await Task.WhenAll(tasks);
    }

    public async Task UpdateUnreadCountAsync(long userId, int unreadCount)
        => await SafeSend("NotificationCountUpdated", _hub.Clients.Group(HubGroups.User(userId.ToString())), new { userId, unreadCount }, $"user-{userId}");

    // ── Presence (simplified — hub marker doesn't track connections) ──

    public bool IsUserOnline(long userId) => false; // Populated by actual Hub
    public IReadOnlyList<string> GetOnlineUserIds() => Array.Empty<string>();

    // ── Private Helper ────────────────────────────────────────────

    private async Task SafeSend(string eventName, IClientProxy target, object payload, string targetDescription)
    {
        try
        {
            await target.SendAsync(eventName, payload);
            _log.LogDebug("SignalR: Sent '{Event}' to {Target}", eventName, targetDescription);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "SignalR: Failed to send '{Event}' to {Target}", eventName, targetDescription);
        }
    }
}

/// <summary>
/// Marker Hub class — allows IHubContext to be registered without a circular dependency on the API Hub.
/// The API's NotificationHub extends this, so IHubContext<NotificationHubMarker> resolves correctly.
/// </summary>
public class NotificationHubMarker : Hub { }
