using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Common.Models;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Tasks.Queries.GetTasks;

/// <summary>Any project role, including Viewer, may list tasks.</summary>
public sealed class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, PagedResult<TaskDto>>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProjectAuthorizationService _authorization;

    public GetTasksQueryHandler(
        ITaskRepository taskRepository, ICurrentUserService currentUserService, ProjectAuthorizationService authorization)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _authorization = authorization;
    }

    public async Task<PagedResult<TaskDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");
        await _authorization.EnsureRoleAsync(request.ProjectId, userId, ProjectRole.Viewer, cancellationToken);

        var filter = new TaskQueryFilter(
            request.ProjectId, request.PageNumber, request.PageSize, request.Status, request.Priority,
            request.Search, request.AssignedToUserId, request.SortBy, request.SortDescending);

        return await _taskRepository.GetForProjectAsync(filter, cancellationToken);
    }
}
