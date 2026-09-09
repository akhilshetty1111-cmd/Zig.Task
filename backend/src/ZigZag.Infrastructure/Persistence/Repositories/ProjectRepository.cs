using Dapper;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Entities;
using ZigZag.Domain.Enums;
using ZigZag.Infrastructure.Persistence.Mapping;

namespace ZigZag.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProjectRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string ProjectSelectSql = """
        SELECT
            p.id,
            p.name,
            p.description,
            p.owner_id AS OwnerId,
            u.name AS OwnerName,
            p.is_archived AS IsArchived,
            p.created_at AS CreatedAt,
            p.updated_at AS UpdatedAt,
            -- ::int: count(*) is bigint/Int64, but Dapper's record materialization
            -- needs an exact type match against MemberCount (int) - see
            -- DbDateTimeMapper's remarks for the general class of bug this is.
            (SELECT count(*)::int FROM project_members pm2 WHERE pm2.project_id = p.id) AS MemberCount
        FROM projects p
        JOIN users u ON u.id = p.owner_id
        """;

    public async Task<ProjectDto?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var sql = ProjectSelectSql + " WHERE p.id = @ProjectId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ProjectRow>(
            new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken));

        return row is null ? null : ToDto(row);
    }

    public async Task<IReadOnlyList<ProjectDto>> GetForUserAsync(
        Guid userId, bool includeArchived, CancellationToken cancellationToken = default)
    {
        var sql = ProjectSelectSql + """

            JOIN project_members pm ON pm.project_id = p.id AND pm.user_id = @UserId
            WHERE (@IncludeArchived OR p.is_archived = false)
            ORDER BY p.created_at DESC;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ProjectRow>(new CommandDefinition(
            sql, new { UserId = userId, IncludeArchived = includeArchived }, cancellationToken: cancellationToken));

        return rows.Select(ToDto).ToList();
    }

    public async Task<Guid> CreateAsync(Project project, CancellationToken cancellationToken = default)
    {
        // Both statements in one round trip: PostgreSQL runs a multi-statement
        // simple-query batch as an implicit transaction, so the project and
        // its Owner membership row either both exist or neither does.
        const string sql = """
            INSERT INTO projects (id, name, description, owner_id, is_archived, created_at, updated_at)
            VALUES (@Id, @Name, @Description, @OwnerId, @IsArchived, @CreatedAt, @UpdatedAt);

            INSERT INTO project_members (project_id, user_id, role)
            VALUES (@Id, @OwnerId, @OwnerRole);
            """;

        var parameters = new
        {
            project.Id,
            project.Name,
            project.Description,
            project.OwnerId,
            project.IsArchived,
            project.CreatedAt,
            project.UpdatedAt,
            OwnerRole = DbEnumMapper.ToDbValue(ProjectRole.Owner),
        };

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return project.Id;
    }

    public async Task UpdateAsync(
        Guid projectId, string name, string? description, CancellationToken cancellationToken = default)
    {
        // updated_at is maintained by the trg_projects_updated_at trigger, not set here.
        const string sql = "UPDATE projects SET name = @Name, description = @Description WHERE id = @ProjectId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { ProjectId = projectId, Name = name, Description = description }, cancellationToken: cancellationToken));
    }

    public async Task ArchiveAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE projects SET is_archived = true WHERE id = @ProjectId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken));
    }

    public async Task<ProjectRole?> GetMemberRoleAsync(
        Guid projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT role FROM project_members WHERE project_id = @ProjectId AND user_id = @UserId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var role = await connection.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition(sql, new { ProjectId = projectId, UserId = userId }, cancellationToken: cancellationToken));

        return role is null ? null : DbEnumMapper.Parse<ProjectRole>(role);
    }

    public async Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(
        Guid projectId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT u.id AS UserId, u.name, u.email, pm.role, pm.joined_at AS JoinedAt
            FROM project_members pm
            JOIN users u ON u.id = pm.user_id
            WHERE pm.project_id = @ProjectId
            ORDER BY pm.joined_at;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ProjectMemberRow>(
            new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken));

        // PascalCase, matching how UserDto.Role is exposed (enum.ToString()) -
        // not the SCREAMING_SNAKE_CASE the database stores.
        return rows.Select(r => new ProjectMemberDto(
            r.UserId, r.Name, r.Email, DbEnumMapper.Parse<ProjectRole>(r.Role).ToString(), DbDateTimeMapper.ToUtcOffset(r.JoinedAt)))
            .ToList();
    }

    public async Task AddMemberAsync(
        Guid projectId, Guid userId, ProjectRole role, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO project_members (project_id, user_id, role) VALUES (@ProjectId, @UserId, @Role);";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { ProjectId = projectId, UserId = userId, Role = DbEnumMapper.ToDbValue(role) }, cancellationToken: cancellationToken));
    }

    public async Task RemoveMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM project_members WHERE project_id = @ProjectId AND user_id = @UserId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { ProjectId = projectId, UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CountOwnersAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT count(*) FROM project_members WHERE project_id = @ProjectId AND role = @OwnerRole;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql, new { ProjectId = projectId, OwnerRole = DbEnumMapper.ToDbValue(ProjectRole.Owner) }, cancellationToken: cancellationToken));
    }

    private static ProjectDto ToDto(ProjectRow row) => new(
        row.Id, row.Name, row.Description, row.OwnerId, row.OwnerName, row.IsArchived,
        row.MemberCount, DbDateTimeMapper.ToUtcOffset(row.CreatedAt), DbDateTimeMapper.ToUtcOffset(row.UpdatedAt));

    // DateTime, not DateTimeOffset, for timestamptz columns - see DbDateTimeMapper.
    private sealed record ProjectRow(
        Guid Id, string Name, string? Description, Guid OwnerId, string OwnerName,
        bool IsArchived, DateTime CreatedAt, DateTime UpdatedAt, int MemberCount);

    private sealed record ProjectMemberRow(Guid UserId, string Name, string Email, string Role, DateTime JoinedAt);
}
