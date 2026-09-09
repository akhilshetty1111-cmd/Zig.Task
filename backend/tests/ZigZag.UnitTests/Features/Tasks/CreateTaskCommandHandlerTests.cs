using NSubstitute;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Application.Features.Tasks.Commands.CreateTask;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Domain.Entities;
using ZigZag.Domain.Enums;

namespace ZigZag.UnitTests.Features.Tasks;

public class CreateTaskCommandHandlerTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private CreateTaskCommandHandler CreateHandler()
        => new(_taskRepository, _projectRepository, _currentUserService, new ProjectAuthorizationService(_projectRepository));

    [Fact]
    public async Task Handle_AssigneeNotAProjectMember_ThrowsConflict()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);
        _projectRepository.GetMemberRoleAsync(projectId, userId, Arg.Any<CancellationToken>()).Returns(ProjectRole.Member);
        _projectRepository.GetMemberRoleAsync(projectId, assigneeId, Arg.Any<CancellationToken>()).Returns((ProjectRole?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new CreateTaskCommand(projectId, "Title", null, TaskPriority.Medium, assigneeId, null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _taskRepository.DidNotReceive().CreateAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ViewerRole_IsForbidden()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);
        _projectRepository.GetMemberRoleAsync(projectId, userId, Arg.Any<CancellationToken>()).Returns(ProjectRole.Viewer);
        var handler = CreateHandler();

        var act = () => handler.Handle(
            new CreateTaskCommand(projectId, "Title", null, TaskPriority.Medium, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesTaskWithTodoStatus()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);
        _projectRepository.GetMemberRoleAsync(projectId, userId, Arg.Any<CancellationToken>()).Returns(ProjectRole.Member);
        TaskItem? created = null;
        _taskRepository.CreateAsync(Arg.Do<TaskItem>(t => created = t), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        _taskRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(_ => created is null
            ? null
            : new TaskDto(created.Id, created.ProjectId, created.Title, created.Description,
                created.Status.ToString(), created.Priority.ToString(), created.AssignedTo, null,
                created.CreatedBy, "Creator", created.DueDate, created.CreatedAt, created.UpdatedAt, null));
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateTaskCommand(projectId, "New task", "desc", TaskPriority.High, null, null), CancellationToken.None);

        result.Status.Should().Be("Todo");
        result.Priority.Should().Be("High");
        created!.CreatedBy.Should().Be(userId);
    }
}
