using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Infrastructure.RealTime;

namespace TaskFlow.Infrastructure;

public static partial class InfrastructureServiceRegistration
{
    /// <summary>
    /// Registers all real-time SignalR services.
    /// Call this from InfrastructureServiceRegistration.AddInfrastructure().
    /// </summary>
    public static IServiceCollection AddRealTimeServices(this IServiceCollection services)
    {
        // NotificationHubService — bridge between app services and SignalR
        // Scoped because it uses IHubContext which is thread-safe singleton
        services.AddScoped<INotificationHubService, NotificationHubService>();

        // Background cleanup worker — runs daily
        services.AddHostedService<NotificationCleanupService>();

        // Background delivery queue — handles reconnect notifications
        services.AddSingleton<NotificationDeliveryService>();
        services.AddHostedService(sp => sp.GetRequiredService<NotificationDeliveryService>());

        return services;
    }
}
