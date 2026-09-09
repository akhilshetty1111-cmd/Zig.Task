using Dapper;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Dashboard.Common;
using ZigZag.Domain.Enums;
using ZigZag.Infrastructure.Persistence.Mapping;

namespace ZigZag.Infrastructure.Persistence.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DashboardRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // One batched round trip - five SELECTs sent together and read back via
    // QueryMultipleAsync - rather than five separate repository calls. This
    // is what "optimized dashboard query" means in practice with Dapper.
    private const string DashboardSql = """
        SELECT count(*)::int FROM project_members WHERE user_id = @UserId;

        SELECT
            count(*)::int AS TotalTasks,
            count(*) FILTER (WHERE t.status = 'DONE')::int AS CompletedTasks,
            count(*) FILTER (WHERE t.status <> 'DONE')::int AS PendingTasks,
            count(*) FILTER (WHERE t.due_date < CURRENT_DATE AND t.status <> 'DONE')::int AS OverdueTasks,
            count(*) FILTER (WHERE t.assigned_to = @UserId)::int AS TasksAssignedToMe
        FROM tasks t
        JOIN project_members pm ON pm.project_id = t.project_id AND pm.user_id = @UserId;

        SELECT t.status AS Status, count(*)::int AS Count
        FROM tasks t
        JOIN project_members pm ON pm.project_id = t.project_id AND pm.user_id = @UserId
        GROUP BY t.status;

        SELECT t.priority AS Priority, count(*)::int AS Count
        FROM tasks t
        JOIN project_members pm ON pm.project_id = t.project_id AND pm.user_id = @UserId
        GROUP BY t.priority;

        SELECT
            th.id,
            th.task_id AS TaskId,
            t.title AS TaskTitle,
            th.changed_by AS ChangedByUserId,
            u.name AS ChangedByName,
            th.field_name AS FieldName,
            th.old_value AS OldValue,
            th.new_value AS NewValue,
            th.changed_at AS ChangedAt
        FROM task_history th
        JOIN tasks t ON t.id = th.task_id
        JOIN project_members pm ON pm.project_id = t.project_id AND pm.user_id = @UserId
        JOIN users u ON u.id = th.changed_by
        ORDER BY th.changed_at DESC
        LIMIT 10;
        """;

    public async Task<DashboardDto> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(DashboardSql, new { UserId = userId }, cancellationToken: cancellationToken));

        var totalProjects = await multi.ReadSingleAsync<int>();
        var taskCounts = await multi.ReadSingleAsync<TaskCountsRow>();
        var statusRows = (await multi.ReadAsync<StatusCountRow>()).ToList();
        var priorityRows = (await multi.ReadAsync<PriorityCountRow>()).ToList();
        var activityRows = (await multi.ReadAsync<ActivityRow>()).ToList();

        var tasksByStatus = statusRows
            .Select(r => new StatusCountDto(DbEnumMapper.Parse<TaskItemStatus>(r.Status).ToString(), r.Count))
            .ToList();

        var tasksByPriority = priorityRows
            .Select(r => new PriorityCountDto(DbEnumMapper.Parse<TaskPriority>(r.Priority).ToString(), r.Count))
            .ToList();

        var recentActivities = activityRows
            .Select(r => new RecentActivityDto(
                r.Id, r.TaskId, r.TaskTitle, r.ChangedByUserId, r.ChangedByName,
                r.FieldName, r.OldValue, r.NewValue, DbDateTimeMapper.ToUtcOffset(r.ChangedAt)))
            .ToList();

        return new DashboardDto(
            totalProjects,
            taskCounts.TotalTasks,
            taskCounts.CompletedTasks,
            taskCounts.PendingTasks,
            taskCounts.OverdueTasks,
            taskCounts.TasksAssignedToMe,
            tasksByStatus,
            tasksByPriority,
            recentActivities);
    }

    private sealed record TaskCountsRow(int TotalTasks, int CompletedTasks, int PendingTasks, int OverdueTasks, int TasksAssignedToMe);

    private sealed record StatusCountRow(string Status, int Count);

    private sealed record PriorityCountRow(string Priority, int Count);

    // DateTime, not DateTimeOffset, for the changed_at timestamptz column -
    // see DbDateTimeMapper's remarks for why.
    private sealed record ActivityRow(
        Guid Id, Guid TaskId, string TaskTitle, Guid ChangedByUserId, string ChangedByName,
        string FieldName, string? OldValue, string? NewValue, DateTime ChangedAt);
}
