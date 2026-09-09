namespace ZigZag.Application.Features.Authentication.Common;

/// <summary>
/// Everything a successful register/login/refresh produces. The controller
/// splits this: AccessToken and User go in the JSON body; RawRefreshToken is
/// set as an HttpOnly cookie and is never returned to client-side JavaScript.
/// </summary>
public sealed record AuthResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RawRefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserDto User);
