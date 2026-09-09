using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZigZag.Application.Features.Authentication.Commands.Login;
using ZigZag.Application.Features.Authentication.Commands.Logout;
using ZigZag.Application.Features.Authentication.Commands.RefreshToken;
using ZigZag.Application.Features.Authentication.Commands.Register;
using ZigZag.Application.Features.Authentication.Common;
using ZigZag.Application.Features.Authentication.Queries.GetCurrentUser;

namespace ZigZag.API.Controllers;

/// <summary>
/// The JSON body returned to the client. Never includes the refresh token -
/// that goes out as an HttpOnly cookie instead, so it is never reachable from
/// client-side JavaScript.
/// </summary>
public sealed record AuthResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, UserDto User);

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    // Scoped to /api/auth so the browser does not attach it to every API
    // request - only the endpoints that actually need it (refresh, logout).
    private const string RefreshTokenCookieName = "refreshToken";
    private const string RefreshTokenCookiePath = "/api/auth";

    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Creates an account and signs the caller in immediately.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterCommand command, CancellationToken cancellationToken)
        => Ok(ToResponseAndSetCookie(await _mediator.Send(command, cancellationToken)));

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginCommand command, CancellationToken cancellationToken)
        => Ok(ToResponseAndSetCookie(await _mediator.Send(command, cancellationToken)));

    /// <summary>Rotates the refresh token (read from the cookie) and issues a new access token.</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        var rawRefreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrEmpty(rawRefreshToken))
        {
            return Unauthorized(new { message = "No refresh token was provided." });
        }

        var result = await _mediator.Send(new RefreshTokenCommand(rawRefreshToken), cancellationToken);
        return Ok(ToResponseAndSetCookie(result));
    }

    /// <summary>
    /// Revokes the refresh token (read from the cookie) and clears it. Not
    /// [Authorize]-gated: an expired access token should not prevent a caller
    /// from clearing their session.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var rawRefreshToken = Request.Cookies[RefreshTokenCookieName];
        await _mediator.Send(new LogoutCommand(rawRefreshToken ?? string.Empty), cancellationToken);

        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = RefreshTokenCookiePath });
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetCurrentUserQuery(), cancellationToken));

    private AuthResponse ToResponseAndSetCookie(AuthResult result)
    {
        Response.Cookies.Append(RefreshTokenCookieName, result.RawRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            // None, not Lax/Strict: the SPA (Azure Static Web Apps) and the API
            // (Azure App Service) are on different domains in production - a
            // genuinely cross-site relationship, not just cross-port like local
            // dev. Lax/Strict would silently stop sending this cookie in
            // production. CSRF exposure from None is limited here: CORS
            // already restricts which origins can read the response, so a
            // forged cross-site request could at most invalidate the victim's
            // session, not obtain a token.
            SameSite = SameSiteMode.None,
            Path = RefreshTokenCookiePath,
            Expires = result.RefreshTokenExpiresAt,
        });

        return new AuthResponse(result.AccessToken, result.AccessTokenExpiresAt, result.User);
    }
}
