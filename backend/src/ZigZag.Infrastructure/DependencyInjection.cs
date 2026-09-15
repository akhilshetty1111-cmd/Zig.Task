using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Attachments.Common;
using ZigZag.Application.Features.Authentication.Common;
using ZigZag.Application.Features.Comments.Common;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Infrastructure.Messaging;
using ZigZag.Infrastructure.Persistence;
using ZigZag.Infrastructure.Persistence.Repositories;
using ZigZag.Infrastructure.Security;
using ZigZag.Infrastructure.Storage;

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
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<BlobStorageSettings>(configuration.GetSection(BlobStorageSettings.SectionName));
        services.Configure<ServiceBusSettings>(configuration.GetSection(ServiceBusSettings.SectionName));

        // Singleton: a ServiceBusClient is meant to be created once and reused
        // for the app's lifetime, not per-request (Azure SDK guidance) - unlike
        // the Scoped repositories above, which need a fresh DB connection per
        // request.
        services.AddSingleton<INotificationMessagePublisher, ServiceBusNotificationPublisher>();

        // Hosted services run at host startup, before any request ever
        // arrives - unlike AzureBlobStorageService (constructed lazily, so a
        // blank config only fails the request that needs it), registering
        // this unconditionally would crash the ENTIRE app on boot in any
        // environment that hasn't configured ServiceBus yet. Only register it
        // once a connection string actually exists.
        var serviceBusConnectionString = configuration.GetSection(ServiceBusSettings.SectionName)["ConnectionString"];
        if (!string.IsNullOrWhiteSpace(serviceBusConnectionString))
        {
            services.AddHostedService<NotificationConsumerBackgroundService>();
        }

        return services;
    }
}
