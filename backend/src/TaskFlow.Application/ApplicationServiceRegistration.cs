using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TaskFlow.Application.Services;

namespace TaskFlow.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AutoMapper — scan this assembly for MappingProfile
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // FluentValidation — scan this assembly for all validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Application Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
