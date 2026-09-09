using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Tasks.Commands.ChangeTaskPriority;

public sealed class ChangeTaskPriorityCommandHandler : IRequestHandler<ChangeTaskPriorityCommand, TaskDto>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskHistoryRepository _taskHistoryRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProjectAuthorizationService _authorization;

    public ChangeTaskPriorityCommandHandler(
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

    public async Task<TaskDto> Handle(ChangeTaskPriorityCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        await _authorization.EnsureRoleAsync(task.ProjectId, userId, ProjectRole.Member, cancellationToken);

        var oldPriority = task.Priority;
        var newPriorityValue = request.NewPriority.ToString();

        if (oldPriority != newPriorityValue)
        {
            await _taskRepository.ChangePriorityAsync(request.TaskId, request.NewPriority, cancellationToken);
            await _taskHistoryRepository.AddAsync(
                request.TaskId, userId, "priority", oldPriority, newPriorityValue, cancellationToken);
        }

        return await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);
    }
}
