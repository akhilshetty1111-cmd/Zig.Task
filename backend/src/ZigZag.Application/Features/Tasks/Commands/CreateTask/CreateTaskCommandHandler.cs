using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Domain.Entities;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Tasks.Commands.CreateTask;

/// <summary>Requires the Member role or higher - a Viewer may not create tasks.</summary>
public sealed class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProjectAuthorizationService _authorization;

    public CreateTaskCommandHandler(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        ICurrentUserService currentUserService,
        ProjectAuthorizationService authorization)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
        _authorization = authorization;
    }

    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");
        await _authorization.EnsureRoleAsync(request.ProjectId, userId, ProjectRole.Member, cancellationToken);

        if (request.AssignedToUserId is { } assigneeId)
        {
            var assigneeRole = await _projectRepository.GetMemberRoleAsync(request.ProjectId, assigneeId, cancellationToken);
            if (assigneeRole is null)
            {
                throw new ConflictException("The assignee must be a member of the project.");
            }
        }

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Title = request.Title,
            Description = request.Description,
            Status = TaskItemStatus.Todo,
            Priority = request.Priority,
            AssignedTo = request.AssignedToUserId,
            CreatedBy = userId,
            DueDate = request.DueDate,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await _taskRepository.CreateAsync(task, cancellationToken);

        return await _taskRepository.GetByIdAsync(task.Id, cancellationToken)
            ?? throw new InvalidOperationException("Task was created but could not be read back.");
    }
}
