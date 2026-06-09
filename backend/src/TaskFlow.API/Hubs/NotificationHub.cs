using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

using TaskFlow.Infrastructure.RealTime;

namespace TaskFlow.API.Hubs;

/*
 * ╔══════════════════════════════════════════════════════════════════╗
 * ║           HOW SIGNALR WORKS — BEGINNER EXPLANATION              ║
 * ╠══════════════════════════════════════════════════════════════════╣
 * ║                                                                  ║
 * ║  Normal HTTP:  Client → Server (one direction, request/response) ║
 * ║  SignalR:      Client ⇄ Server (both directions, persistent)     ║
 * ║                                                                  ║
 * ║  Think of it like a phone call vs. mailing a letter:            ║
 * ║    - HTTP    = mailing a letter (send → wait → reply)           ║
 * ║    - SignalR = phone call       (both talk anytime)             ║
 * ║                                                                  ║
 * ║  GROUPS: Like WhatsApp groups.                                  ║
 * ║    "project-5" group = everyone working on Project 5 gets       ║
 * ║    real-time task updates when something changes in Project 5   ║
 * ║                                                                  ║
 * ║  CONNECTIONS: Each browser tab = one unique connection.         ║
 * ║    A user can have MULTIPLE connections (multiple tabs).         ║
 * ║    We track all connections per user in UserConnections dict.   ║
 * ║                                                                  ║
 * ╚══════════════════════════════════════════════════════════════════╝
 *
 * SERVER → CLIENT EVENTS (the frontend listens for these):
 * ─────────────────────────────────────────────────────────
 *   NewNotification          → personal notification for ONE user
 *   TaskCreated              → new task in a project
 *   TaskUpdated              → task details changed
 *   TaskStatusChanged        → task moved to a new Kanban column
 *   TaskAssigned             → task was assigned to someone
 *   CommentAdded             → new comment on a task
 *   ProjectMemberAdded       → someone joined a project
 *   UserPresenceChanged      → user came online / went offline
 *   NotificationCountUpdated → badge number on the bell icon changed
 *
 * CLIENT → SERVER METHODS (the frontend calls these):
 * ─────────────────────────────────────────────────────────
 *   JoinProject(projectId)   → subscribe to project updates
 *   LeaveProject(projectId)  → unsubscribe
 *   JoinTask(taskId)         → subscribe to task comment feed
 *   LeaveTask(taskId)        → unsubscribe
 *   MarkNotificationRead(id) → mark a notification read via WebSocket
 */

[Authorize]
public class NotificationHub : NotificationHubMarker
{
    private readonly ILogger<NotificationHub> _log;

    /// <summary>
    /// In-memory tracking of which users are online and which connections they have.
    ///
    /// Key   = userId (string)
    /// Value = set of connectionIds (one per browser tab)
    ///
    /// NOTE: In a multi-server (scaled) deployment, replace this with
    ///       Redis or a distributed cache (IDistributedCache).
    /// </summary>
    private static readonly ConcurrentDictionary<string, HashSet<string>> UserConnections = new();

    public NotificationHub(ILogger<NotificationHub> log) => _log = log;

    // ── OnConnected ─────────────────────────────────────────────
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            Context.Abort(); // Reject unauthenticated connections
            return;
        }

        // Track this connection
        UserConnections.AddOrUpdate(
            userId,
            _ => [Context.ConnectionId],
            (_, set) => { lock (set) { set.Add(Context.ConnectionId); } return set; }
        );

        // Join personal group — server can now send messages to "user-{userId}"
        await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroup(userId));

        // Broadcast online presence to all users in this user's projects
        // (other users see the green dot light up)
        await Clients.Others.SendAsync("UserPresenceChanged", new
        {
            UserId    = userId,
            IsOnline  = true,
            ConnectedAt = DateTime.UtcNow,
        });

        _log.LogInformation(
            "SignalR: User {UserId} connected [ConnectionId={ConnId}] [TotalConnections={Total}]",
            userId, Context.ConnectionId, GetConnectionCount(userId));

        await base.OnConnectedAsync();
    }

    // ── OnDisconnected ──────────────────────────────────────────
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            // Remove this specific connection (user might still have other tabs open)
            if (UserConnections.TryGetValue(userId, out var connections))
            {
                lock (connections) { connections.Remove(Context.ConnectionId); }

                // Only broadcast "offline" if ALL connections closed (all tabs closed)
                if (connections.Count == 0)
                {
                    UserConnections.TryRemove(userId, out _);
                    await Clients.Others.SendAsync("UserPresenceChanged", new
                    {
                        UserId       = userId,
                        IsOnline     = false,
                        DisconnectedAt = DateTime.UtcNow,
                    });
                }
            }

            _log.LogInformation(
                "SignalR: User {UserId} disconnected [ConnectionId={ConnId}] [Reason={Error}]",
                userId, Context.ConnectionId, exception?.Message ?? "clean");
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ── CLIENT-CALLABLE METHODS ─────────────────────────────────

    /// <summary>Join a project group to receive real-time task events.</summary>
    public async Task JoinProject(long projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetProjectGroup(projectId));
        _log.LogDebug("User {UserId} joined project group {ProjectId}", GetUserId(), projectId);
    }

    /// <summary>Leave a project group (navigated away from project).</summary>
    public async Task LeaveProject(long projectId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetProjectGroup(projectId));

    /// <summary>Subscribe to comment events on a specific task.</summary>
    public async Task JoinTask(long taskId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, GetTaskGroup(taskId));

    /// <summary>Unsubscribe from task comment events.</summary>
    public async Task LeaveTask(long taskId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetTaskGroup(taskId));

    /// <summary>
    /// Client informs server that a notification was read.
    /// Server updates the DB and broadcasts the new unread count.
    /// </summary>
    public Task MarkNotificationRead(long notificationId)
    {
        // This will be handled by NotificationHubService via IHubContext
        // (keeping Hub thin — no service calls directly in Hub methods)
        _log.LogDebug("User {UserId} marked notification {NotifId} as read via SignalR",
            GetUserId(), notificationId);
        return Task.CompletedTask;
    }

    // ── PRESENCE HELPERS ────────────────────────────────────────

    /// <summary>Returns whether a user currently has at least one active connection.</summary>
    public static bool IsUserOnline(string userId)
        => UserConnections.ContainsKey(userId) && UserConnections[userId].Count > 0;

    /// <summary>Returns all online user IDs.</summary>
    public static IReadOnlyList<string> GetOnlineUserIds()
        => UserConnections.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key).ToList();

    /// <summary>Gets the number of active connections for a user (open tabs).</summary>
    public static int GetConnectionCount(string userId)
        => UserConnections.TryGetValue(userId, out var c) ? c.Count : 0;

    // ── GROUP NAME HELPERS ───────────────────────────────────────
    public static string GetUserGroup(string userId)       => $"user-{userId}";
    public static string GetProjectGroup(long projectId)   => $"project-{projectId}";
    public static string GetTaskGroup(long taskId)         => $"task-{taskId}";

    private string? GetUserId() => Context.User?.FindFirst("uid")?.Value;
}
