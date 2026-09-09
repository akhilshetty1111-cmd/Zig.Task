using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Tasks.Commands.DeleteTask;

/// <summary>
/// Requires Manager+ - a permanent delete is more destructive than editing or
/// changing status/priority/assignee (all Member-level), so it gets the
/// higher bar already used for project-level destructive actions.
/// </summary>
public sealed class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProjectAuthorizationService _authorization;

    public DeleteTaskCommandHandler(
        ITaskRepository taskRepository, ICurrentUserService currentUserService, ProjectAuthorizationService authorization)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _authorization = authorization;
    }

    public async Task Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        await _authorization.EnsureRoleAsync(task.ProjectId, userId, ProjectRole.Manager, cancellationToken);

        await _taskRepository.DeleteAsync(request.TaskId, cancellationToken);
    }
}
