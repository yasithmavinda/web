using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Interfaces.Repositories;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Persistence.Repositories;
using TaskFlow.Infrastructure.Security;
using TaskFlow.Infrastructure.Services;

namespace TaskFlow.Infrastructure;

public static partial class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // ── EF Core / SQL Server ──────────────────────────────
        services.AddDbContext<TaskFlowDbContext>(opts =>
            opts.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));

        // ── Settings ──────────────────────────────────────────
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<SecuritySettings>(configuration.GetSection(SecuritySettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        // Also register Application-layer settings (same appsettings sections)
        services.Configure<TaskFlow.Application.Common.Settings.JwtSettings>(configuration.GetSection(TaskFlow.Infrastructure.Security.JwtSettings.SectionName));
        services.Configure<TaskFlow.Application.Common.Settings.SecuritySettings>(configuration.GetSection(TaskFlow.Infrastructure.Security.SecuritySettings.SectionName));

        // ── Repositories ──────────────────────────────────────
        services.AddScoped<IUserRepository,                UserRepository>();
        services.AddScoped<IRefreshTokenRepository,        RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository,  PasswordResetTokenRepository>();
        services.AddScoped<IAuditLogRepository,            AuditLogRepository>();
        services.AddScoped<IProjectRepository,             ProjectRepository>();
        services.AddScoped<ITaskRepository,                TaskRepository>();
        services.AddScoped<ICommentRepository,             CommentRepository>();
        services.AddScoped<INotificationRepository,        NotificationRepository>();

        // ── Infrastructure Services ───────────────────────────
        services.AddScoped<IJwtService,         JwtService>();
        services.AddScoped<IPasswordHasher,     PasswordHasher>();
        services.AddScoped<IEmailService,       EmailService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHttpContextAccessor();
        // ── RealTime SignalR Services ────────────────────────
        services.AddRealTimeServices();

        return services;
    }
}
