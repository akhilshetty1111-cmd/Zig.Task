using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ZigZag.Application.Common.Behaviors;

/// <summary>
/// Logs the name and elapsed time of every command and query, and re-logs any
/// exception the handler let through before rethrowing.
/// </summary>
/// <remarks>
/// Logs only the request TYPE NAME, never its properties - a command like
/// RegisterUserCommand carries a password, and CreateTaskCommand might carry
/// arbitrary user-entered text. Per docs/architecture.md and the "Logging"
/// requirements, passwords/tokens are never logged; logging properties
/// generically here would make that a matter of every future command
/// remembering to exclude sensitive fields, which is exactly the kind of
/// thing this behavior exists to make automatic instead.
/// Registered as the outermost pipeline behavior (see
/// DependencyInjection.AddApplication) so it captures validation failures too.
/// </remarks>
public sealed partial class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            LogHandled(requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            LogFailed(ex, requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {RequestName} in {ElapsedMilliseconds}ms")]
    private partial void LogHandled(string requestName, long elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{RequestName} failed after {ElapsedMilliseconds}ms")]
    private partial void LogFailed(Exception exception, string requestName, long elapsedMilliseconds);
}
