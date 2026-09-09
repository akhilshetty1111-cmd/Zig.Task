using NSubstitute;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Comments.Commands.DeleteComment;
using ZigZag.Application.Features.Comments.Common;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.UnitTests.Features.Comments;

public class DeleteCommentCommandHandlerTests
{
    private readonly ICommentRepository _commentRepository = Substitute.For<ICommentRepository>();
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();

    private DeleteCommentCommandHandler CreateHandler()
        => new(_commentRepository, _taskRepository, _currentUserService, _projectRepository);

    [Fact]
    public async Task Handle_AuthorDeletingOwnComment_Succeeds_WithoutCheckingProjectRole()
    {
        var commentId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        _currentUserService.UserId.Returns(authorId);
        _commentRepository.GetByIdAsync(commentId, Arg.Any<CancellationToken>())
            .Returns(new CommentDto(commentId, Guid.NewGuid(), authorId, "Author", "text", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var handler = CreateHandler();

        await handler.Handle(new DeleteCommentCommand(commentId), CancellationToken.None);

        await _commentRepository.Received(1).DeleteAsync(commentId, Arg.Any<CancellationToken>());
        // Author path never needs to look up the task/project role.
        await _taskRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonAuthorWithInsufficientRole_ThrowsForbidden()
    {
        var commentId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        _currentUserService.UserId.Returns(callerId);
        _commentRepository.GetByIdAsync(commentId, Arg.Any<CancellationToken>())
            .Returns(new CommentDto(commentId, taskId, authorId, "Author", "text", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        _taskRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>())
            .Returns(new TaskDto(taskId, projectId, "T", null, "Todo", "Medium", null, null, authorId, "Author", null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null));
        _projectRepository.GetMemberRoleAsync(projectId, callerId, Arg.Any<CancellationToken>()).Returns(ProjectRole.Member);
        var handler = CreateHandler();

        var act = () => handler.Handle(new DeleteCommentCommand(commentId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        await _commentRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonAuthorButManager_Succeeds_Moderation()
    {
        var commentId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        _currentUserService.UserId.Returns(callerId);
        _commentRepository.GetByIdAsync(commentId, Arg.Any<CancellationToken>())
            .Returns(new CommentDto(commentId, taskId, authorId, "Author", "text", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        _taskRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>())
            .Returns(new TaskDto(taskId, projectId, "T", null, "Todo", "Medium", null, null, authorId, "Author", null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null));
        _projectRepository.GetMemberRoleAsync(projectId, callerId, Arg.Any<CancellationToken>()).Returns(ProjectRole.Manager);
        var handler = CreateHandler();

        await handler.Handle(new DeleteCommentCommand(commentId), CancellationToken.None);

        await _commentRepository.Received(1).DeleteAsync(commentId, Arg.Any<CancellationToken>());
    }
}
