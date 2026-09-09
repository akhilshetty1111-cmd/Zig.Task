using ZigZag.Application.Common.Exceptions;
using ValidationException = FluentValidation.ValidationException;

namespace ZigZag.API.Common;

/// <summary>
/// Maps an exception to the HTTP status code and <see cref="ApiErrorResponse"/>
/// body the global exception handler writes.
/// </summary>
/// <remarks>
/// Deliberately a pure function with no dependency on <c>HttpContext</c> or
/// logging, so the exception -&gt; status code policy can be unit tested
/// directly rather than only through a full HTTP round trip. See
/// <c>ZigZag.API.Middleware.GlobalExceptionHandler</c> for the side-effecting
/// wrapper that actually writes the response and logs.
/// </remarks>
public static class ExceptionToApiResponseMapper
{
    /// <param name="exception">The exception the pipeline let through.</param>
    /// <param name="traceId">Correlation ID echoed back in the response body.</param>
    /// <param name="includeExceptionDetails">
    /// Whether to include the raw exception message for an unmapped exception.
    /// Pass <c>true</c> only in Development - see the "Never expose internal
    /// exception details in production" security requirement. Mapped
    /// exceptions (NotFound, Forbidden, Conflict, validation) always use their
    /// own message regardless of this flag, since those messages are written
    /// by application code specifically to be shown to a caller.
    /// </param>
    public static (int StatusCode, ApiErrorResponse Body) Map(
        Exception exception, string traceId, bool includeExceptionDetails)
    {
        return exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                new ApiErrorResponse(
                    "Validation failed.",
                    validationException.Errors.Select(e => e.ErrorMessage).ToList(),
                    traceId)),

            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                new ApiErrorResponse(notFound.Message, [], traceId)),

            ForbiddenAccessException forbidden => (
                StatusCodes.Status403Forbidden,
                new ApiErrorResponse(forbidden.Message, [], traceId)),

            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                new ApiErrorResponse(conflict.Message, [], traceId)),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ApiErrorResponse(
                    includeExceptionDetails ? exception.Message : "An unexpected error occurred.",
                    [],
                    traceId)),
        };
    }
}
