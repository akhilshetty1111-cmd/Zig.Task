using NSubstitute;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Application.Features.Tasks.Commands.ChangeTaskStatus;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.UnitTests.Features.Tasks;

public class ChangeTaskStatusCommandHandlerTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly ITaskHistoryRepository _taskHistoryRepository = Substitute.For<ITaskHistoryRepository>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private ChangeTaskStatusCommandHandler CreateHandler()
        => new(_taskRepository, _taskHistoryRepository, _currentUserService, new ProjectAuthorizationService(_projectRepository));

    private static TaskDto SampleTask(Guid projectId, Guid taskId, string status = "Todo") => new(
        taskId, projectId, "Sample", null, status, "Medium", null, null,
        Guid.NewGuid(), "Creator", null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);

    [Fact]
    public async Task Handle_StatusActuallyChanges_UpdatesAndRecordsHistory()
    {
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);
        _projectRepository.GetMemberRoleAsync(projectId, userId, Arg.Any<CancellationToken>()).Returns(ProjectRole.Member);
        _taskRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>()).Returns(SampleTask(projectId, taskId, "Todo"));
        var handler = CreateHandler();

        await handler.Handle(new ChangeTaskStatusCommand(taskId, TaskItemStatus.InProgress), CancellationToken.None);

        await _taskRepository.Received(1).ChangeStatusAsync(taskId, TaskItemStatus.InProgress, Arg.Any<CancellationToken>());
        await _taskHistoryRepository.Received(1).AddAsync(
            taskId, userId, "status", "Todo", "InProgress", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StatusUnchanged_DoesNotWriteOrRecordHistory()
    {
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);
        _projectRepository.GetMemberRoleAsync(projectId, userId, Arg.Any<CancellationToken>()).Returns(ProjectRole.Member);
        // Task is already InProgress; command asks to set it to InProgress again.
        _taskRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>()).Returns(SampleTask(projectId, taskId, "InProgress"));
        var handler = CreateHandler();

        await handler.Handle(new ChangeTaskStatusCommand(taskId, TaskItemStatus.InProgress), CancellationToken.None);

        await _taskRepository.DidNotReceive().ChangeStatusAsync(Arg.Any<Guid>(), Arg.Any<TaskItemStatus>(), Arg.Any<CancellationToken>());
        await _taskHistoryRepository.DidNotReceive().AddAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ViewerRole_IsForbidden()
    {
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);
        _projectRepository.GetMemberRoleAsync(projectId, userId, Arg.Any<CancellationToken>()).Returns(ProjectRole.Viewer);
        _taskRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>()).Returns(SampleTask(projectId, taskId, "Todo"));
        var handler = CreateHandler();

        var act = () => handler.Handle(new ChangeTaskStatusCommand(taskId, TaskItemStatus.Done), CancellationToken.None);

        await act.Should().ThrowAsync<ZigZag.Application.Common.Exceptions.ForbiddenAccessException>();
    }
}
