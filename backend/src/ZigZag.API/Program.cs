using System.Reflection;
using Microsoft.OpenApi.Models;
using Serilog;
using ZigZag.API.Middleware;
using ZigZag.Application;
using ZigZag.Infrastructure;

namespace ZigZag.API;

/// <summary>
/// Composition root for the ZigZag API.
/// </summary>
/// <remarks>
/// PHASE 3 SCOPE: adds MediatR/CQRS, the validation and logging pipeline
/// behaviors, and global exception handling on top of the Phase 1/2
/// foundation. JWT authentication is added in Phase 4.
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
        builder.Services.AddSwaggerGen(ConfigureSwagger);
        builder.Services.AddHealthChecks();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        // Required by the exception handler middleware's own startup
        // validation even though it's never reached in practice: GlobalExceptionHandler
        // always returns true (it maps every exception, including a catch-all
        // for unmapped ones), so this fallback JSON:API-style ProblemDetails
        // formatter never actually runs - but UseExceptionHandler() throws at
        // startup if neither this, an ExceptionHandlingPath, nor an
        // ExceptionHandler delegate is configured. Confirmed by actually
        // running the app: it crashed on startup without this line.
        builder.Services.AddProblemDetails();
        builder.Services.AddInfrastructure();
        builder.Services.AddApplication();

        AddConfiguredCors(builder);

        var app = builder.Build();

        // No-argument overload: runs the IExceptionHandler(s) registered via
        // AddExceptionHandler<GlobalExceptionHandler>() above, rather than the
        // older lambda/error-path forms of this middleware. Must be first so it
        // catches everything downstream, including exceptions from Serilog's
        // own request-logging middleware.
        app.UseExceptionHandler();

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

    /// <summary>
    /// Metadata and XML-comment wiring only. The JWT bearer security scheme
    /// (the "Authorize" button, per the "Swagger" requirement in the spec) is
    /// added in Phase 4 alongside the login endpoint - configuring it before
    /// there is any way to obtain a token would just be dead UI.
    /// </summary>
    private static void ConfigureSwagger(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "ZigZag API",
            Version = "v1",
            Description = "Project and task management API - Kanban boards, task workflows, comments and a reporting dashboard.",
        });

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
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
