using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ZigZag.Application.Common.Behaviors;
using ZigZag.Application.Features.Authentication.Common;

namespace ZigZag.Application;

/// <summary>
/// Composition entry point for the Application layer, called once from
/// ZigZag.API's Program.cs.
/// </summary>
public static class DependencyInjection
{
    private static readonly Assembly ApplicationAssembly = typeof(ApplicationAssemblyMarker).Assembly;

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(ApplicationAssembly);

            // Registration order is nesting order: the first behavior registered
            // is outermost. Logging wraps Validation so it observes (and logs)
            // validation failures too, not just handler exceptions.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(ApplicationAssembly);

        // Not a MediatR handler or FluentValidation validator, so assembly
        // scanning above never finds it - registered explicitly instead.
        services.AddScoped<AuthTokenIssuer>();

        return services;
    }
}
