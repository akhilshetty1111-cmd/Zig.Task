using Dapper;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Common.Models;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Domain.Entities;
using ZigZag.Domain.Enums;
using ZigZag.Infrastructure.Persistence.Mapping;

namespace ZigZag.Infrastructure.Persistence.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TaskRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string TaskColumnsSql = """
        t.id,
        t.project_id AS ProjectId,
        t.title,
        t.description,
        t.status,
        t.priority,
        t.assigned_to AS AssignedToUserId,
        au.name AS AssignedToName,
        t.created_by AS CreatedByUserId,
        cu.name AS CreatedByName,
        t.due_date AS DueDate,
        t.created_at AS CreatedAt,
        t.updated_at AS UpdatedAt,
        t.completed_at AS CompletedAt
        """;

    private const string TaskJoinsSql = """
        FROM tasks t
        JOIN users cu ON cu.id = t.created_by
        LEFT JOIN users au ON au.id = t.assigned_to
        """;

    public async Task<TaskDto?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT {TaskColumnsSql} {TaskJoinsSql} WHERE t.id = @TaskId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<TaskRow>(
            new CommandDefinition(sql, new { TaskId = taskId }, cancellationToken: cancellationToken));

        return row is null ? null : ToDto(row);
    }

    public async Task<PagedResult<TaskDto>> GetForProjectAsync(
        TaskQueryFilter filter, CancellationToken cancellationToken = default)
    {
        var whereClauses = new List<string> { "t.project_id = @ProjectId" };
        var parameters = new DynamicParameters();
        parameters.Add("ProjectId", filter.ProjectId);

        if (filter.Status is { } status)
        {
            whereClauses.Add("t.status = @Status");
            parameters.Add("Status", DbEnumMapper.ToDbValue(status));
        }

        if (filter.Priority is { } priority)
        {
            whereClauses.Add("t.priority = @Priority");
            parameters.Add("Priority", DbEnumMapper.ToDbValue(priority));
        }

        if (filter.AssignedToUserId is { } assignedTo)
        {
            whereClauses.Add("t.assigned_to = @AssignedToUserId");
            parameters.Add("AssignedToUserId", assignedTo);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            // The VALUE is always bound via @Search, never concatenated into
            // the SQL text - only the WHERE clause's STRUCTURE varies with
            // which filters are present.
            whereClauses.Add("(t.title ILIKE @Search OR t.description ILIKE @Search)");
            parameters.Add("Search", $"%{filter.Search}%");
        }

        // Priority is stored as text ('LOW'/'MEDIUM'/'HIGH'/'URGENT'), which
        // sorts alphabetically wrong (HIGH before LOW) - a CASE expression
        // orders by actual urgency instead.
        var orderColumn = filter.SortBy switch
        {
            TaskSortBy.DueDate => "t.due_date",
            TaskSortBy.Priority => "CASE t.priority " +
                "WHEN 'LOW' THEN 0 WHEN 'MEDIUM' THEN 1 WHEN 'HIGH' THEN 2 WHEN 'URGENT' THEN 3 END",
            TaskSortBy.Title => "t.title",
            _ => "t.created_at",
        };
        var direction = filter.SortDescending ? "DESC" : "ASC";
        // Tasks with no due date sink to the bottom regardless of sort
        // direction, instead of jumping to the top on a DESC sort (Postgres's
        // default NULL ordering).
        var nullsClause = filter.SortBy == TaskSortBy.DueDate ? "NULLS LAST" : "";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", (filter.PageNumber - 1) * filter.PageSize);

        var sql = $"""
            SELECT {TaskColumnsSql}, count(*) OVER()::int AS TotalCount
            {TaskJoinsSql}
            WHERE {string.Join(" AND ", whereClauses)}
            ORDER BY {orderColumn} {direction} {nullsClause}
            LIMIT @PageSize OFFSET @Offset;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = (await connection.QueryAsync<TaskRowWithCount>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))).ToList();

        // count(*) OVER() produces no rows at all when nothing matches,
        // rather than a single row with TotalCount = 0.
        var totalCount = rows.Count > 0 ? rows[0].TotalCount : 0;
        var items = rows.Select(r => ToDto(r.ToTaskRow())).ToList();

        return new PagedResult<TaskDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<Guid> CreateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO tasks (id, project_id, title, description, status, priority, assigned_to, created_by, due_date, created_at, updated_at)
            VALUES (@Id, @ProjectId, @Title, @Description, @Status, @Priority, @AssignedTo, @CreatedBy, @DueDate, @CreatedAt, @UpdatedAt);
            """;

        var parameters = new
        {
            task.Id,
            task.ProjectId,
            task.Title,
            task.Description,
            Status = DbEnumMapper.ToDbValue(task.Status),
            Priority = DbEnumMapper.ToDbValue(task.Priority),
            task.AssignedTo,
            task.CreatedBy,
            DueDate = task.DueDate.HasValue ? task.DueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
            task.CreatedAt,
            task.UpdatedAt,
        };

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return task.Id;
    }

    public async Task UpdateAsync(
        Guid taskId, string title, string? description, DateOnly? dueDate, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE tasks SET title = @Title, description = @Description, due_date = @DueDate WHERE id = @TaskId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            TaskId = taskId,
            Title = title,
            Description = description,
            DueDate = dueDate.HasValue ? dueDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
        }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM tasks WHERE id = @TaskId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { TaskId = taskId }, cancellationToken: cancellationToken));
    }

    public async Task ChangeStatusAsync(
        Guid taskId, TaskItemStatus newStatus, CancellationToken cancellationToken = default)
    {
        // completed_at is set/cleared in the same statement as status - the
        // tasks_completed_at_matches_status CHECK constraint requires both to
        // change together, and this is the only place status ever changes.
        const string sql = """
            UPDATE tasks
            SET status = @Status,
                completed_at = CASE WHEN @Status = 'DONE' THEN now() ELSE NULL END
            WHERE id = @TaskId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { TaskId = taskId, Status = DbEnumMapper.ToDbValue(newStatus) }, cancellationToken: cancellationToken));
    }

    public async Task ChangeAssigneeAsync(
        Guid taskId, Guid? assignedToUserId, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE tasks SET assigned_to = @AssignedTo WHERE id = @TaskId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { TaskId = taskId, AssignedTo = assignedToUserId }, cancellationToken: cancellationToken));
    }

    public async Task ChangePriorityAsync(
        Guid taskId, TaskPriority newPriority, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE tasks SET priority = @Priority WHERE id = @TaskId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { TaskId = taskId, Priority = DbEnumMapper.ToDbValue(newPriority) }, cancellationToken: cancellationToken));
    }

    private static TaskDto ToDto(TaskRow row) => new(
        row.Id, row.ProjectId, row.Title, row.Description,
        DbEnumMapper.Parse<TaskItemStatus>(row.Status).ToString(),
        DbEnumMapper.Parse<TaskPriority>(row.Priority).ToString(),
        row.AssignedToUserId, row.AssignedToName, row.CreatedByUserId, row.CreatedByName,
        row.DueDate.HasValue ? DateOnly.FromDateTime(row.DueDate.Value) : null,
        DbDateTimeMapper.ToUtcOffset(row.CreatedAt), DbDateTimeMapper.ToUtcOffset(row.UpdatedAt),
        row.CompletedAt.HasValue ? DbDateTimeMapper.ToUtcOffset(row.CompletedAt.Value) : null);

    // DateTime, not DateTimeOffset/DateOnly, for timestamptz/date columns -
    // see DbDateTimeMapper's remarks for why.
    private sealed record TaskRow(
        Guid Id, Guid ProjectId, string Title, string? Description, string Status, string Priority,
        Guid? AssignedToUserId, string? AssignedToName, Guid CreatedByUserId, string CreatedByName,
        DateTime? DueDate, DateTime CreatedAt, DateTime UpdatedAt, DateTime? CompletedAt);

    // A fully independent record, not inheriting from TaskRow, so Dapper's
    // constructor-matching has exactly one flat parameter list to resolve
    // rather than something layered through a base call.
    private sealed record TaskRowWithCount(
        Guid Id, Guid ProjectId, string Title, string? Description, string Status, string Priority,
        Guid? AssignedToUserId, string? AssignedToName, Guid CreatedByUserId, string CreatedByName,
        DateTime? DueDate, DateTime CreatedAt, DateTime UpdatedAt, DateTime? CompletedAt, int TotalCount)
    {
        public TaskRow ToTaskRow() => new(
            Id, ProjectId, Title, Description, Status, Priority, AssignedToUserId, AssignedToName,
            CreatedByUserId, CreatedByName, DueDate, CreatedAt, UpdatedAt, CompletedAt);
    }
}
