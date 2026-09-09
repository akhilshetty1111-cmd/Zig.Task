using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Domain.Entities;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Tasks.Commands.ChangeTaskAssignee;

public sealed class ChangeTaskAssigneeCommandHandler : IRequestHandler<ChangeTaskAssigneeCommand, TaskDto>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskHistoryRepository _taskHistoryRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProjectAuthorizationService _authorization;

    public ChangeTaskAssigneeCommandHandler(
        ITaskRepository taskRepository,
        ITaskHistoryRepository taskHistoryRepository,
        IProjectRepository projectRepository,
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService,
        ProjectAuthorizationService authorization)
    {
        _taskRepository = taskRepository;
        _taskHistoryRepository = taskHistoryRepository;
        _projectRepository = projectRepository;
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
        _authorization = authorization;
    }

    public async Task<TaskDto> Handle(ChangeTaskAssigneeCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        await _authorization.EnsureRoleAsync(task.ProjectId, userId, ProjectRole.Member, cancellationToken);

        if (request.AssignedToUserId is { } assigneeId)
        {
            var assigneeRole = await _projectRepository.GetMemberRoleAsync(task.ProjectId, assigneeId, cancellationToken);
            if (assigneeRole is null)
            {
                throw new ConflictException("The assignee must be a member of the project.");
            }
        }

        if (task.AssignedToUserId != request.AssignedToUserId)
        {
            await _taskRepository.ChangeAssigneeAsync(request.TaskId, request.AssignedToUserId, cancellationToken);
            await _taskHistoryRepository.AddAsync(
                request.TaskId, userId, "assigned_to",
                task.AssignedToUserId?.ToString(), request.AssignedToUserId?.ToString(), cancellationToken);

            // Notify the new assignee, unless they assigned it to themselves.
            if (request.AssignedToUserId is { } newAssigneeId && newAssigneeId != userId)
            {
                await _notificationRepository.CreateAsync(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = newAssigneeId,
                    Type = "TASK_ASSIGNED",
                    Title = $"You were assigned to \"{task.Title}\"",
                    Message = null,
                    TaskId = task.Id,
                    ProjectId = task.ProjectId,
                    IsRead = false,
                    CreatedAt = DateTimeOffset.UtcNow,
                }, cancellationToken);
            }
        }

        return await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);
    }
}
