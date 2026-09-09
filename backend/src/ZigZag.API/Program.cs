using Serilog;
using ZigZag.Infrastructure;

namespace ZigZag.API;

/// <summary>
/// Composition root for the ZigZag API.
/// </summary>
/// <remarks>
/// PHASE 1 SCOPE: this wires up only what is needed for the API to start, log,
/// serve Swagger and answer a health probe. MediatR/CQRS registration, global
/// exception middleware, FluentValidation pipeline behaviors and JWT
/// authentication are added in Phase 3 and Phase 4.
/// </remarks>
public class Program
{
    /// <summary>Application entry point.</summary>
    public static int Main(string[] args)
    {
        // A bootstrap logger captures failures that happen BEFORE configuration is
        // read. Without it, a bad connection string or malformed appsettings would
        // crash the host with no log line at all.
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting ZigZag API host");
            BuildApp(args).Run();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "ZigZag API host terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static WebApplication BuildApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Replace the bootstrap logger with the fully configured one. Sinks,
        // levels and enrichers come from the "Serilog" section of appsettings so
        // logging can be retuned per environment without a redeploy.
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();
        builder.Services.AddInfrastructure();

        AddConfiguredCors(builder);

        var app = builder.Build();

        // Structured request logging: one enriched line per request instead of the
        // three noisy default lines from the framework.
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors(CorsPolicyName);
        app.MapControllers();

        // Two paths on purpose:
        //   /health     - infrastructure probe (Azure App Service, Docker HEALTHCHECK).
        //   /api/health - reachable through the SPA's configured API base URL, so the
        //                 frontend can verify connectivity without special-casing it.
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/api/health");

        return app;
    }

    private const string CorsPolicyName = "ZigZagCors";

    /// <summary>
    /// Reads allowed origins from configuration rather than hardcoding them.
    /// AllowAnyOrigin() is deliberately never used: the API issues credentials,
    /// and a wildcard origin combined with AllowCredentials is rejected by browsers
    /// anyway. Dev origins live in appsettings.Development.json; production origins
    /// come from Azure App Service configuration.
    /// </summary>
    private static void AddConfiguredCors(WebApplicationBuilder builder)
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        builder.Services.AddCors(options => options.AddPolicy(CorsPolicyName, policy =>
        {
            if (allowedOrigins.Length == 0)
            {
                // Fail closed. An empty policy blocks cross-origin calls instead of
                // silently opening the API because someone forgot to set the setting.
                Log.Warning("No Cors:AllowedOrigins configured - cross-origin requests will be blocked");
                return;
            }

            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }));
    }
}
