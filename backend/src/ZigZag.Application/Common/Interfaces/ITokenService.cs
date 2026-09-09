using ZigZag.Domain.Entities;

namespace ZigZag.Application.Common.Interfaces;

/// <summary>An issued JWT access token and when it stops being valid.</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>
/// Issues JWT access tokens and opaque refresh tokens. Implemented in
/// Infrastructure - Application never touches a JWT or crypto library
/// directly.
/// </summary>
/// <remarks>
/// Access tokens (short-lived, signed, stateless) and refresh tokens
/// (long-lived, opaque, stored server-side as a hash) are different
/// technologies but are always issued and consumed together by the
/// Authentication feature, so one interface covers both rather than
/// splitting into two for a single small feature.
/// </remarks>
public interface ITokenService
{
    AccessToken CreateAccessToken(User user);

    /// <summary>A cryptographically random opaque value - never a JWT, never derived from user data.</summary>
    string GenerateRefreshToken();

    /// <summary>
    /// SHA-256 of a raw refresh token, for storage/lookup. A raw refresh token
    /// already has enough entropy that a fast hash is appropriate here -
    /// unlike <see cref="IPasswordHasher"/>, which exists specifically because
    /// user-chosen passwords do NOT have enough entropy for a fast hash to be
    /// safe. BCrypt's per-hash random salt would also make an indexed lookup
    /// by hash impossible, which this needs.
    /// </summary>
    string HashRefreshToken(string rawToken);

    TimeSpan RefreshTokenLifetime { get; }
}
