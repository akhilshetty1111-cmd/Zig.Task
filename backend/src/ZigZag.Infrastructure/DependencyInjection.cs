using Microsoft.Extensions.DependencyInjection;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Infrastructure.Persistence;

namespace ZigZag.Infrastructure;

/// <summary>
/// Composition entry point for the Infrastructure layer, called once from
/// ZigZag.API's Program.cs.
/// </summary>
/// <remarks>
/// PHASE 2 SCOPE: persistence only - the connection factory. Repository
/// registrations, JWT services and password hashing are added here in Phase 4
/// alongside authentication.
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Scoped: one connection factory instance per HTTP request, matching
        // the lifetime repositories will be registered with in Phase 4-6, so a
        // request that touches multiple repositories can share one connection
        // for transactional command handlers.
        services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();

        return services;
    }
}
