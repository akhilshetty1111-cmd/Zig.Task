using ZigZag.Application.Common.Interfaces;
using ZigZag.Domain.Entities;

namespace ZigZag.Application.Features.Authentication.Common;

/// <summary>
/// Issues an access/refresh token pair for a user and persists the refresh
/// token's hash. Shared by Register and Login - both end with exactly this
/// step, so it lives once here instead of twice. RefreshTokenCommandHandler
/// does not use this: rotation needs the new token's hash before revoking the
/// old one, which this helper's all-at-once flow doesn't expose.
/// </summary>
/// <remarks>
/// Public rather than internal: it is a constructor parameter of the public
/// RegisterCommandHandler and LoginCommandHandler, and DI-resolved
/// constructor parameters must be at least as accessible as the constructor
/// itself.
/// </remarks>
public sealed class AuthTokenIssuer
{
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AuthTokenIssuer(ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository)
    {
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthResult> IssueAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = _tokenService.CreateAccessToken(user);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashRefreshToken(rawRefreshToken);
        var refreshTokenExpiresAt = DateTimeOffset.UtcNow.Add(_tokenService.RefreshTokenLifetime);

        await _refreshTokenRepository.AddAsync(user.Id, refreshTokenHash, refreshTokenExpiresAt, cancellationToken);

        return new AuthResult(
            accessToken.Value,
            accessToken.ExpiresAt,
            rawRefreshToken,
            refreshTokenExpiresAt,
            new UserDto(user.Id, user.Name, user.Email, user.Role.ToString()));
    }
}
