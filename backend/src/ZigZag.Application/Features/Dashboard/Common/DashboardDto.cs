namespace ZigZag.Application.Features.Dashboard.Common;

public sealed record DashboardDto(
    int TotalProjects,
    int TotalTasks,
    int CompletedTasks,
    int PendingTasks,
    int OverdueTasks,
    int TasksAssignedToMe,
    IReadOnlyList<StatusCountDto> TasksByStatus,
    IReadOnlyList<PriorityCountDto> TasksByPriority,
    IReadOnlyList<RecentActivityDto> RecentActivities);

public sealed record StatusCountDto(string Status, int Count);

public sealed record PriorityCountDto(string Priority, int Count);

public sealed record RecentActivityDto(
    Guid Id, Guid TaskId, string TaskTitle, Guid ChangedByUserId, string ChangedByName,
    string FieldName, string? OldValue, string? NewValue, DateTimeOffset ChangedAt);
