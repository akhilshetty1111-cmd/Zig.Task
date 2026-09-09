using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Comments.Common;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Comments.Commands.DeleteComment;

/// <summary>The comment's own author may delete it; so may a project Manager+ (moderation).</summary>
public sealed class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand>
{
    private readonly ICommentRepository _commentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProjectRepository _projectRepository;

    public DeleteCommentCommandHandler(
        ICommentRepository commentRepository,
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        IProjectRepository projectRepository)
    {
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _projectRepository = projectRepository;
    }

    public async Task Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");

        var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken)
            ?? throw new NotFoundException("Comment", request.CommentId);

        if (comment.UserId != userId)
        {
            var task = await _taskRepository.GetByIdAsync(comment.TaskId, cancellationToken)
                ?? throw new NotFoundException("Task", comment.TaskId);
            var role = await _projectRepository.GetMemberRoleAsync(task.ProjectId, userId, cancellationToken);

            if (role is null or < ProjectRole.Manager)
            {
                throw new ForbiddenAccessException("You may only delete your own comments.");
            }
        }

        await _commentRepository.DeleteAsync(request.CommentId, cancellationToken);
    }
}
