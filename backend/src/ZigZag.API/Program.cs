using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using ZigZag.API.Middleware;
using ZigZag.API.Services;
using ZigZag.Application;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Infrastructure;

namespace ZigZag.API;

/// <summary>
/// Composition root for the ZigZag API.
/// </summary>
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
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddApplication();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        AddConfiguredAuthentication(builder);

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

        // Authentication before Authorization, both after CORS: CORS decides
        // whether the browser even lets the request through; authentication
        // establishes who is calling; authorization decides what they may do.
        app.UseAuthentication();
        app.UseAuthorization();

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
    /// Metadata, XML comments, and the JWT bearer security scheme: login in
    /// Swagger, copy the accessToken from the response, paste it into the
    /// "Authorize" dialog, then call any [Authorize]-protected endpoint.
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

        const string bearerScheme = "Bearer";
        options.AddSecurityDefinition(bearerScheme, new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste only the access token - Swagger adds the \"Bearer \" prefix itself.",
        });

        // Applied globally rather than per-[Authorize]-endpoint via an
        // IOperationFilter: simpler, and a lock icon on an endpoint that
        // doesn't need auth is harmless, unlike the reverse.
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = bearerScheme },
                },
                []
            },
        });
    }

    /// <summary>
    /// JWT bearer authentication. Reads the same "Jwt" configuration keys
    /// that Infrastructure's JwtTokenService uses to ISSUE tokens - both
    /// sides must agree on Key/Issuer/Audience or validation fails for a
    /// token this same API just handed out.
    /// </summary>
    private static void AddConfiguredAuthentication(WebApplicationBuilder builder)
    {
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var key = jwtSection["Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key is not configured. Set it via appsettings, user secrets, or the Jwt__Key environment variable.");

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };

                // Without this, a missing/invalid/expired token produces an
                // empty 401 body - the JwtBearer handler short-circuits before
                // the request ever reaches GlobalExceptionHandler, so the
                // standard ApiErrorResponse envelope never gets applied.
                // Confirmed by actually calling a protected endpoint with no
                // token: the response really did come back with an empty body.
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        return WriteUnauthorizedResponse(context.HttpContext, "Authentication is required.");
                    },
                    OnForbidden = context => WriteUnauthorizedResponse(
                        context.HttpContext, "You do not have permission to perform this action.", StatusCodes.Status403Forbidden),
                };
            });

        builder.Services.AddAuthorization();
    }

    private static Task WriteUnauthorizedResponse(
        HttpContext httpContext, string message, int statusCode = StatusCodes.Status401Unauthorized)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        var traceId = System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier;
        return httpContext.Response.WriteAsJsonAsync(
            new ZigZag.API.Common.ApiErrorResponse(message, [], traceId));
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
