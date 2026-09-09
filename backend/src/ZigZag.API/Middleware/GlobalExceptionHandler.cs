using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using ZigZag.API.Common;

namespace ZigZag.API.Middleware;

/// <summary>
/// Catches every exception that reaches the end of the request pipeline,
/// logs it, and writes the mapped <see cref="ApiErrorResponse"/> instead of
/// letting ASP.NET Core's default (HTML, in Development) error page through.
/// </summary>
/// <remarks>
/// Registered via <c>AddExceptionHandler&lt;GlobalExceptionHandler&gt;()</c> +
/// <c>app.UseExceptionHandler()</c> in Program.cs - the ASP.NET Core 8+
/// <see cref="IExceptionHandler"/> pattern, which runs as regular middleware
/// rather than needing a separate error-handling endpoint.
/// </remarks>
public sealed partial class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Prefer the W3C trace ID (matches Application Insights / OpenTelemetry
        // correlation) and fall back to ASP.NET Core's own per-request ID if no
        // Activity is running.
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var (statusCode, body) = ExceptionToApiResponseMapper.Map(
            exception, traceId, includeExceptionDetails: _environment.IsDevelopment());

        // 5xx is an application fault and worth an Error-level alert; 4xx here
        // is expected traffic (a 404, a validation failure) and only needs a
        // Warning-level trail for debugging, not to page anyone.
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            LogServerError(exception, traceId, statusCode);
        }
        else
        {
            LogClientError(exception, traceId, statusCode);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);

        return true;
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unhandled exception (traceId={TraceId}, statusCode={StatusCode})")]
    private partial void LogServerError(Exception exception, string traceId, int statusCode);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Request failed (traceId={TraceId}, statusCode={StatusCode})")]
    private partial void LogClientError(Exception exception, string traceId, int statusCode);
}
