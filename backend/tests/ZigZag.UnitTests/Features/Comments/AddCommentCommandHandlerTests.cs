using NSubstitute;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Comments.Commands.AddComment;
using ZigZag.Application.Features.Comments.Common;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Domain.Entities;
using ZigZag.Domain.Enums;

namespace ZigZag.UnitTests.Features.Comments;

public class AddCommentCommandHandlerTests
{
    private readonly ICommentRepository _commentRepository = Substitute.For<ICommentRepository>();
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private AddCommentCommandHandler CreateHandler()
        => new(_commentRepository, _taskRepository, _notificationRepository, _currentUserService,
            new ProjectAuthorizationService(_projectRepository));

    private static TaskDto SampleTask(Guid projectId, Guid taskId, Guid createdBy, Guid? assignedTo) => new(
        taskId, projectId, "Sample task", null, "Todo", "Medium", assignedTo, "Assignee",
        createdBy, "Creator", null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);

    private void SetupCreateAndReadBack(Guid commenterId, Guid taskId)
    {
        _commentRepository.CreateAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        _commentRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new CommentDto(Guid.NewGuid(), taskId, commenterId, "Commenter", "text", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task Handle_NotifiesAssigneeAndCreator_ButNeverTheCommenterThemselves()
    {
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        _currentUserService.UserId.Returns(creatorId); // the CREATOR is commenting
        _projectRepository.GetMemberRoleAsync(projectId, creatorId, Arg.Any<CancellationToken>()).Returns(ProjectRole.Member);
        _taskRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>())
            .Returns(SampleTask(projectId, taskId, creatorId, assigneeId));
        SetupCreateAndReadBack(creatorId, taskId);
        var handler = CreateHandler();

        await handler.Handle(new AddCommentCommand(taskId, "hello"), CancellationToken.None);

        // Assignee gets notified.
        await _notificationRepository.Received(1).CreateAsync(
            Arg.Is<Notification>(n => n.UserId == assigneeId), Arg.Any<CancellationToken>());
        // The creator (who is also the commenter here) must NOT notify themselves.
        await _notificationRepository.DidNotReceive().CreateAsync(
            Arg.Is<Notification>(n => n.UserId == creatorId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AssigneeAndCreatorAreTheSamePerson_OnlyOneNotificationSent()
    {
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var creatorAndAssigneeId = Guid.NewGuid();
        var commenterId = Guid.NewGuid();
        _currentUserService.UserId.Returns(commenterId);
        _projectRepository.GetMemberRoleAsync(projectId, commenterId, Arg.Any<CancellationToken>()).Returns(ProjectRole.Member);
        _taskRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>())
            .Returns(SampleTask(projectId, taskId, creatorAndAssigneeId, creatorAndAssigneeId));
        SetupCreateAndReadBack(commenterId, taskId);
        var handler = CreateHandler();

        await handler.Handle(new AddCommentCommand(taskId, "hello"), CancellationToken.None);

        // A HashSet dedupes creator == assignee, so exactly one notification.
        await _notificationRepository.Received(1).CreateAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }
}
