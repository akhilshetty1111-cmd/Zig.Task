using Dapper;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Infrastructure.Persistence.Mapping;

namespace ZigZag.Infrastructure.Persistence.Repositories;

public sealed class TaskHistoryRepository : ITaskHistoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TaskHistoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(
        Guid taskId, Guid changedByUserId, string fieldName, string? oldValue, string? newValue,
        CancellationToken cancellationToken = default)
    {
        // id, changed_at: left to the column defaults in 001_initial_schema.sql.
        const string sql = """
            INSERT INTO task_history (task_id, changed_by, field_name, old_value, new_value)
            VALUES (@TaskId, @ChangedByUserId, @FieldName, @OldValue, @NewValue);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            TaskId = taskId,
            ChangedByUserId = changedByUserId,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
        }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<TaskHistoryEntryDto>> GetForTaskAsync(
        Guid taskId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                th.id,
                th.changed_by AS ChangedByUserId,
                u.name AS ChangedByName,
                th.field_name AS FieldName,
                th.old_value AS OldValue,
                th.new_value AS NewValue,
                th.changed_at AS ChangedAt
            FROM task_history th
            JOIN users u ON u.id = th.changed_by
            WHERE th.task_id = @TaskId
            ORDER BY th.changed_at DESC;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<TaskHistoryRow>(
            new CommandDefinition(sql, new { TaskId = taskId }, cancellationToken: cancellationToken));

        return rows.Select(r => new TaskHistoryEntryDto(
            r.Id, r.ChangedByUserId, r.ChangedByName, r.FieldName, r.OldValue, r.NewValue,
            DbDateTimeMapper.ToUtcOffset(r.ChangedAt)))
            .ToList();
    }

    // DateTime, not DateTimeOffset, for the changed_at timestamptz column -
    // see DbDateTimeMapper's remarks for why.
    private sealed record TaskHistoryRow(
        Guid Id, Guid ChangedByUserId, string ChangedByName, string FieldName, string? OldValue, string? NewValue, DateTime ChangedAt);
}
