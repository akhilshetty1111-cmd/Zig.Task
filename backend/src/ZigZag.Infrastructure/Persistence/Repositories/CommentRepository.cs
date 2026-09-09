using Dapper;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Comments.Common;
using ZigZag.Domain.Entities;
using ZigZag.Infrastructure.Persistence.Mapping;

namespace ZigZag.Infrastructure.Persistence.Repositories;

public sealed class CommentRepository : ICommentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CommentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string CommentSelectSql = """
        SELECT
            c.id,
            c.task_id AS TaskId,
            c.user_id AS UserId,
            u.name AS UserName,
            c.comment AS Text,
            c.created_at AS CreatedAt,
            c.updated_at AS UpdatedAt
        FROM task_comments c
        JOIN users u ON u.id = c.user_id
        """;

    public async Task<CommentDto?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        var sql = CommentSelectSql + " WHERE c.id = @CommentId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<CommentRow>(
            new CommandDefinition(sql, new { CommentId = commentId }, cancellationToken: cancellationToken));

        return row is null ? null : ToDto(row);
    }

    public async Task<IReadOnlyList<CommentDto>> GetForTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var sql = CommentSelectSql + " WHERE c.task_id = @TaskId ORDER BY c.created_at ASC;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<CommentRow>(
            new CommandDefinition(sql, new { TaskId = taskId }, cancellationToken: cancellationToken));

        return rows.Select(ToDto).ToList();
    }

    public async Task<Guid> CreateAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO task_comments (id, task_id, user_id, comment, created_at, updated_at)
            VALUES (@Id, @TaskId, @UserId, @Text, @CreatedAt, @UpdatedAt);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, comment, cancellationToken: cancellationToken));

        return comment.Id;
    }

    public async Task DeleteAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM task_comments WHERE id = @CommentId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { CommentId = commentId }, cancellationToken: cancellationToken));
    }

    private static CommentDto ToDto(CommentRow row) => new(
        row.Id, row.TaskId, row.UserId, row.UserName, row.Text,
        DbDateTimeMapper.ToUtcOffset(row.CreatedAt), DbDateTimeMapper.ToUtcOffset(row.UpdatedAt));

    // DateTime, not DateTimeOffset, for timestamptz columns - see DbDateTimeMapper.
    private sealed record CommentRow(Guid Id, Guid TaskId, Guid UserId, string UserName, string Text, DateTime CreatedAt, DateTime UpdatedAt);
}
