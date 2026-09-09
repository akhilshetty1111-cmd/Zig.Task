using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Authentication.Common;

namespace ZigZag.Application.Features.Authentication.Commands.RefreshToken;

/// <summary>
/// Rotates a refresh token: the presented token is revoked and a brand new
/// access/refresh pair is issued. If the presented token was already rotated
/// once before (it has a <c>ReplacedByTokenHash</c>), presenting it again
/// means it may have leaked - the legitimate client would be using the token
/// it was rotated into, not this one - so every active token for the user is
/// revoked instead of just this one.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    private const string InvalidTokenMessage = "Invalid refresh token.";

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository, IUserRepository userRepository, ITokenService tokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var presentedHash = _tokenService.HashRefreshToken(request.RawRefreshToken);
        var record = await _refreshTokenRepository.GetByTokenHashAsync(presentedHash, cancellationToken);

        if (record is null)
        {
            throw new AuthenticationFailedException(InvalidTokenMessage);
        }

        if (record.IsRevoked)
        {
            if (record.WasReused)
            {
                await _refreshTokenRepository.RevokeAllActiveForUserAsync(record.UserId, cancellationToken);
                throw new AuthenticationFailedException(
                    "This refresh token has already been used. All sessions have been revoked for security - please sign in again.");
            }

            throw new AuthenticationFailedException("This refresh token has been revoked.");
        }

        if (record.IsExpired)
        {
            throw new AuthenticationFailedException("This refresh token has expired. Please sign in again.");
        }

        var user = await _userRepository.GetByIdAsync(record.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new AuthenticationFailedException(InvalidTokenMessage);
        }

        var accessToken = _tokenService.CreateAccessToken(user);
        var newRawRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _tokenService.HashRefreshToken(newRawRefreshToken);
        var newRefreshTokenExpiresAt = DateTimeOffset.UtcNow.Add(_tokenService.RefreshTokenLifetime);

        // Revoke the old token (linked to the new one via ReplacedByTokenHash)
        // before persisting the new one: if the process crashes between these
        // two calls, the caller ends up needing to sign in again rather than
        // having both the old and new tokens simultaneously valid.
        await _refreshTokenRepository.RevokeAsync(presentedHash, newRefreshTokenHash, cancellationToken);
        await _refreshTokenRepository.AddAsync(user.Id, newRefreshTokenHash, newRefreshTokenExpiresAt, cancellationToken);

        return new AuthResult(
            accessToken.Value,
            accessToken.ExpiresAt,
            newRawRefreshToken,
            newRefreshTokenExpiresAt,
            new UserDto(user.Id, user.Name, user.Email, user.Role.ToString()));
    }
}
