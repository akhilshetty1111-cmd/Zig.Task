namespace ZigZag.Application.Common.Exceptions;

/// <summary>
/// Thrown by a handler when a request conflicts with the current state of the
/// resource - a duplicate email on register, a concurrent edit. Mapped to 409
/// by the API's global exception handler.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
