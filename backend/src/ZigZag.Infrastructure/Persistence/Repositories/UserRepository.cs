using Dapper;
using Npgsql;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Domain.Entities;
using ZigZag.Domain.Enums;
using ZigZag.Infrastructure.Persistence.Mapping;

namespace ZigZag.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                name,
                email,
                password_hash AS PasswordHash,
                role,
                is_active AS IsActive,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM users
            WHERE id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : ToDomain(row);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                name,
                email,
                password_hash AS PasswordHash,
                role,
                is_active AS IsActive,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM users
            WHERE email = @Email;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        // citext makes this comparison case-insensitive at the database level -
        // no ToLower() needed here.
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));

        return row is null ? null : ToDomain(row);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM users WHERE email = @Email);";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
    }

    public async Task<Guid> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO users (id, name, email, password_hash, role, is_active, created_at, updated_at)
            VALUES (@Id, @Name, @Email, @PasswordHash, @Role, @IsActive, @CreatedAt, @UpdatedAt);
            """;

        var parameters = new
        {
            user.Id,
            user.Name,
            user.Email,
            user.PasswordHash,
            Role = DbEnumMapper.ToDbValue(user.Role),
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
        };

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // Defense in depth against the race the handler's own
            // EmailExistsAsync pre-check cannot close: two concurrent
            // registrations for the same email. The database's UNIQUE
            // constraint on citext email is the actual source of truth.
            throw new ConflictException("An account with this email already exists.");
        }

        return user.Id;
    }

    private static User ToDomain(UserRow row) => new()
    {
        Id = row.Id,
        Name = row.Name,
        Email = row.Email,
        PasswordHash = row.PasswordHash,
        Role = DbEnumMapper.Parse<UserRole>(row.Role),
        IsActive = row.IsActive,
        CreatedAt = DbDateTimeMapper.ToUtcOffset(row.CreatedAt),
        UpdatedAt = DbDateTimeMapper.ToUtcOffset(row.UpdatedAt),
    };

    // DateTime, not DateTimeOffset, for the timestamptz columns - see
    // DbDateTimeMapper's remarks for why.
    private sealed record UserRow(
        Guid Id,
        string Name,
        string Email,
        string PasswordHash,
        string Role,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
