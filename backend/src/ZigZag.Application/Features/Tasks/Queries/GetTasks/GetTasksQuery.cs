using MediatR;
using ZigZag.Application.Common.Models;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Tasks.Queries.GetTasks;

public sealed record GetTasksQuery(
    Guid ProjectId,
    int PageNumber,
    int PageSize,
    TaskItemStatus? Status,
    TaskPriority? Priority,
    string? Search,
    Guid? AssignedToUserId,
    TaskSortBy SortBy,
    bool SortDescending) : IRequest<PagedResult<TaskDto>>;
