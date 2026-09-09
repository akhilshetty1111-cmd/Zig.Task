namespace ZigZag.Application.Common.Exceptions;

/// <summary>
/// Thrown by a handler when a requested entity does not exist, or the caller
/// is not authorized to know that it does. Mapped to 404 by the API's global
/// exception handler.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}
