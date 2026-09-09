namespace ZigZag.Application.Common.Exceptions;

/// <summary>
/// Thrown by a handler when the caller is authenticated but not permitted to
/// perform the requested action - e.g. a project member without the required
/// project role. Mapped to 403 by the API's global exception handler.
/// </summary>
/// <remarks>
/// Distinct from an authentication failure (401), which the JWT middleware
/// itself produces before a handler ever runs.
/// </remarks>
public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("You do not have permission to perform this action.")
    {
    }

    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
