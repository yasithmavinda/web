using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Infrastructure.RealTime;

/// <summary>
/// NotificationCleanupService — Background worker that runs every 30 days.
///
/// Beginner: A "Background Service" is like a cron job inside your app.
/// It runs independently while your API handles requests.
///
/// This service:
///   1. Deletes read notifications older than 30 days
///   2. Deletes any notifications older than 90 days (even if unread)
///
/// This prevents the Notifications table from growing forever.
/// </summary>
public class NotificationCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationCleanupService> _log;
    private readonly TimeSpan _interval = TimeSpan.FromDays(1); // Run daily

    public NotificationCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationCleanupService> log)
    { _scopeFactory = scopeFactory; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("NotificationCleanupService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);

            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "NotificationCleanupService: Error during cleanup.");
            }
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();

        var cutoffRead   = DateTime.UtcNow.AddDays(-30);
        var cutoffAll    = DateTime.UtcNow.AddDays(-90);

        // Delete read notifications older than 30 days
        int deletedRead = await db.Notifications
            .Where(n => n.IsRead && n.CreatedAt < cutoffRead)
            .ExecuteDeleteAsync(ct);

        // Delete ALL notifications older than 90 days
        int deletedOld = await db.Notifications
            .Where(n => n.CreatedAt < cutoffAll)
            .ExecuteDeleteAsync(ct);

        if (deletedRead > 0 || deletedOld > 0)
        {
            _log.LogInformation(
                "NotificationCleanup: Deleted {Read} read + {Old} old notifications.",
                deletedRead, deletedOld);
        }
    }
}

/// <summary>
/// NotificationDeliveryService — Handles "offline notification delivery".
///
/// Problem: User A assigns a task to User B while User B is OFFLINE.
///   Step 1: NotificationService saves notification to DB  (always happens)
///   Step 2: NotificationHubService.SendAsync() → silently does nothing (user offline)
///   Step 3: User B comes back online → frontend calls GET /notifications/unread
///   Step 4: Sees the notification from the DB ✅
///
/// This service handles Step 1.5 — it also fires an immediate push when a
/// user RECONNECTS, so they get their pending notifications instantly.
///
/// Implementation: The Hub's OnConnectedAsync triggers this via IHubContext.
/// We use a Channel (producer/consumer queue) to avoid blocking the Hub.
/// </summary>
public class NotificationDeliveryService : BackgroundService
{
    private readonly System.Threading.Channels.Channel<(long UserId, string ConnectionId)> _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationDeliveryService> _log;

    public NotificationDeliveryService(IServiceScopeFactory scopeFactory, ILogger<NotificationDeliveryService> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
        // Bounded channel — if queue fills up, oldest items are dropped
        _queue = System.Threading.Channels.Channel.CreateBounded<(long, string)>(
            new System.Threading.Channels.BoundedChannelOptions(500)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.DropOldest,
            });
    }

    /// <summary>
    /// Called by NotificationHub.OnConnectedAsync via DI.
    /// Queues a "send pending count" job for the reconnected user.
    /// </summary>
    public void QueueReconnectDelivery(long userId, string connectionId)
        => _queue.Writer.TryWrite((userId, connectionId));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var (userId, connectionId) in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await DeliverPendingCountAsync(userId, connectionId, stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to deliver pending notifications to user {UserId}", userId);
            }
        }
    }

    private async Task DeliverPendingCountAsync(long userId, string connectionId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db          = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
        var hubService  = scope.ServiceProvider.GetRequiredService<INotificationHubService>();

        int unreadCount = await db.Notifications
            .CountAsync(n => n.RecipientId == userId && !n.IsRead, ct);

        if (unreadCount > 0)
        {
            await hubService.UpdateUnreadCountAsync(userId, unreadCount);
            _log.LogDebug("Sent pending unread count {Count} to reconnected user {UserId}", unreadCount, userId);
        }
    }
}
