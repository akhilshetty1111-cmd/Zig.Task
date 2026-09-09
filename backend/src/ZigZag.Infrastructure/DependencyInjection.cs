using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Authentication.Common;
using ZigZag.Application.Features.Comments.Common;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Infrastructure.Persistence;
using ZigZag.Infrastructure.Persistence.Repositories;
using ZigZag.Infrastructure.Security;

namespace ZigZag.Infrastructure;

/// <summary>
/// Composition entry point for the Infrastructure layer, called once from
/// ZigZag.API's Program.cs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Scoped: one connection factory instance per HTTP request, so a
        // request that touches multiple repositories could share one
        // connection for transactional command handlers (Phase 6+).
        services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITaskHistoryRepository, TaskHistoryRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        return services;
    }
}
