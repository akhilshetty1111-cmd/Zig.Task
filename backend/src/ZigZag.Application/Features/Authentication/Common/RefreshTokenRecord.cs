namespace ZigZag.Application.Features.Authentication.Common;

/// <summary>A row read back from the refresh_tokens table.</summary>
public sealed record RefreshTokenRecord(
    Guid Id,
    Guid UserId,
    string TokenHash,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    string? ReplacedByTokenHash)
{
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    public bool IsRevoked => RevokedAt is not null;

    /// <summary>
    /// True when this token was already consumed by a prior refresh (rotated
    /// into a new one) and is now being presented again - the signal that a
    /// stolen/leaked token is in use, since the legitimate client would be
    /// using the token it was rotated into instead.
    /// </summary>
    public bool WasReused => ReplacedByTokenHash is not null;
}
