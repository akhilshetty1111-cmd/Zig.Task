using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Tasks.Common;

public enum TaskSortBy
{
    CreatedAt,
    DueDate,
    Priority,
    Title,
}

/// <summary>Every filter/sort/paging input for listing a project's tasks - one parameter object, not seven loose ones.</summary>
public sealed record TaskQueryFilter(
    Guid ProjectId,
    int PageNumber,
    int PageSize,
    TaskItemStatus? Status,
    TaskPriority? Priority,
    string? Search,
    Guid? AssignedToUserId,
    TaskSortBy SortBy,
    bool SortDescending);
