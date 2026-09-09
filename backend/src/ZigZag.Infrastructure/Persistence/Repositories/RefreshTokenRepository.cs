using Dapper;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Authentication.Common;
using ZigZag.Infrastructure.Persistence.Mapping;

namespace ZigZag.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RefreshTokenRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(
        Guid userId, string tokenHash, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        // id, created_at: left to the column defaults in 001_initial_schema.sql
        // rather than set here.
        const string sql = """
            INSERT INTO refresh_tokens (user_id, token_hash, expires_at)
            VALUES (@UserId, @TokenHash, @ExpiresAt);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { UserId = userId, TokenHash = tokenHash, ExpiresAt = expiresAt }, cancellationToken: cancellationToken));
    }

    public async Task<RefreshTokenRecord?> GetByTokenHashAsync(
        string tokenHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                token_hash AS TokenHash,
                expires_at AS ExpiresAt,
                revoked_at AS RevokedAt,
                replaced_by_token_hash AS ReplacedByTokenHash
            FROM refresh_tokens
            WHERE token_hash = @TokenHash;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<RefreshTokenRow>(
            new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: cancellationToken));

        return row is null
            ? null
            : new RefreshTokenRecord(
                row.Id,
                row.UserId,
                row.TokenHash,
                DbDateTimeMapper.ToUtcOffset(row.ExpiresAt),
                row.RevokedAt is { } revokedAt ? DbDateTimeMapper.ToUtcOffset(revokedAt) : null,
                row.ReplacedByTokenHash);
    }

    // DateTime, not DateTimeOffset, for the timestamptz columns - see
    // DbDateTimeMapper's remarks for why RefreshTokenRecord itself (which
    // Application uses and which stays DateTimeOffset) cannot be the direct
    // Dapper materialization target.
    private sealed record RefreshTokenRow(
        Guid Id,
        Guid UserId,
        string TokenHash,
        DateTime ExpiresAt,
        DateTime? RevokedAt,
        string? ReplacedByTokenHash);

    public async Task RevokeAsync(
        string tokenHash, string? replacedByTokenHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE refresh_tokens
            SET revoked_at = now(), replaced_by_token_hash = @ReplacedByTokenHash
            WHERE token_hash = @TokenHash AND revoked_at IS NULL;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { TokenHash = tokenHash, ReplacedByTokenHash = replacedByTokenHash }, cancellationToken: cancellationToken));
    }

    public async Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE refresh_tokens
            SET revoked_at = now()
            WHERE user_id = @UserId AND revoked_at IS NULL;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }
}
