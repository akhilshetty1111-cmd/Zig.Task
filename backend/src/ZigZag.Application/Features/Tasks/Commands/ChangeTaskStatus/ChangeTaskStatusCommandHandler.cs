using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Tasks.Commands.ChangeTaskStatus;

/// <summary>
/// The Kanban drag-and-drop endpoint: Frontend -&gt; PATCH status -&gt; this
/// handler -&gt; Dapper -&gt; PostgreSQL -&gt; task_history, per the flow this was
/// specified against. Records a task_history row on every real change.
/// </summary>
public sealed class ChangeTaskStatusCommandHandler : IRequestHandler<ChangeTaskStatusCommand, TaskDto>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskHistoryRepository _taskHistoryRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProjectAuthorizationService _authorization;

    public ChangeTaskStatusCommandHandler(
        ITaskRepository taskRepository,
        ITaskHistoryRepository taskHistoryRepository,
        ICurrentUserService currentUserService,
        ProjectAuthorizationService authorization)
    {
        _taskRepository = taskRepository;
        _taskHistoryRepository = taskHistoryRepository;
        _currentUserService = currentUserService;
        _authorization = authorization;
    }

    public async Task<TaskDto> Handle(ChangeTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        await _authorization.EnsureRoleAsync(task.ProjectId, userId, ProjectRole.Member, cancellationToken);

        var oldStatus = task.Status;
        var newStatusValue = request.NewStatus.ToString();

        if (oldStatus != newStatusValue)
        {
            await _taskRepository.ChangeStatusAsync(request.TaskId, request.NewStatus, cancellationToken);
            await _taskHistoryRepository.AddAsync(
                request.TaskId, userId, "status", oldStatus, newStatusValue, cancellationToken);
        }

        return await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);
    }
}
