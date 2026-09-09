namespace ZigZag.Application.Common.Exceptions;

/// <summary>
/// Thrown for a failed login attempt or an invalid/expired/revoked refresh
/// token. Mapped to 401 by the API's global exception handler.
/// </summary>
/// <remarks>
/// Always carries a message safe to show a caller. For login specifically,
/// callers must use the SAME message regardless of whether the email exists,
/// the password is wrong, or the account is deactivated - a different
/// message per case lets an attacker enumerate registered emails.
/// </remarks>
public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message)
    {
    }
}
