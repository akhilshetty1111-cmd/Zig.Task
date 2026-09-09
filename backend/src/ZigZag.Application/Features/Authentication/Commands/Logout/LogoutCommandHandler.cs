using MediatR;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Authentication.Common;

namespace ZigZag.Application.Features.Authentication.Commands.Logout;

/// <summary>
/// Revokes a refresh token. Deliberately tolerant of an unknown or
/// already-revoked token rather than throwing - logout must be idempotent,
/// and must never reveal whether a given token existed.
/// </summary>
public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository, ITokenService tokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RawRefreshToken))
        {
            return;
        }

        var hash = _tokenService.HashRefreshToken(request.RawRefreshToken);
        var record = await _refreshTokenRepository.GetByTokenHashAsync(hash, cancellationToken);

        if (record is not null && !record.IsRevoked)
        {
            await _refreshTokenRepository.RevokeAsync(hash, replacedByTokenHash: null, cancellationToken);
        }
    }
}
