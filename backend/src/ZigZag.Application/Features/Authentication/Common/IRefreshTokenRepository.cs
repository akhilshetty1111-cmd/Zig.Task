namespace ZigZag.Application.Features.Authentication.Common;

/// <summary>
/// Refresh token persistence. Scoped to Features/Authentication - unlike
/// IUserRepository, nothing outside this feature needs it.
/// </summary>
public interface IRefreshTokenRepository
{
    Task AddAsync(Guid userId, string tokenHash, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);

    Task<RefreshTokenRecord?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <param name="tokenHash">The token being revoked.</param>
    /// <param name="replacedByTokenHash">
    /// Set when this call is a rotation (this token is being replaced by a new
    /// one); left null for an explicit logout, so <see cref="RefreshTokenRecord.WasReused"/>
    /// can distinguish "revoked by rotation" from "revoked by logout".
    /// </param>
    /// <param name="cancellationToken">Cancels the underlying database call.</param>
    Task RevokeAsync(string tokenHash, string? replacedByTokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes every still-active refresh token for a user - the reuse-detection
    /// response: a rotated token being presented again means it may have been
    /// stolen, so the whole session family is invalidated rather than just the
    /// one token.
    /// </summary>
    Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
