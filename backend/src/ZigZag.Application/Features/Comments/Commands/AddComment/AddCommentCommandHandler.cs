using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Comments.Common;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Entities;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Comments.Commands.AddComment;

/// <summary>
/// Notifies the task's assignee and creator (if not the commenter) by
/// publishing to Service Bus rather than writing a notification row
/// directly - see <see cref="INotificationMessagePublisher"/> and
/// NotificationConsumerBackgroundService for the other half of this.
/// </summary>
public sealed class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, CommentDto>
{
    private readonly ICommentRepository _commentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly INotificationMessagePublisher _notificationPublisher;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProjectAuthorizationService _authorization;

    public AddCommentCommandHandler(
        ICommentRepository commentRepository,
        ITaskRepository taskRepository,
        INotificationMessagePublisher notificationPublisher,
        ICurrentUserService currentUserService,
        ProjectAuthorizationService authorization)
    {
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
        _notificationPublisher = notificationPublisher;
        _currentUserService = currentUserService;
        _authorization = authorization;
    }

    public async Task<CommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        await _authorization.EnsureRoleAsync(task.ProjectId, userId, ProjectRole.Member, cancellationToken);

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            UserId = userId,
            Text = request.Text,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await _commentRepository.CreateAsync(comment, cancellationToken);

        await NotifyInterestedPartiesAsync(task, userId, cancellationToken);

        return await _commentRepository.GetByIdAsync(comment.Id, cancellationToken)
            ?? throw new InvalidOperationException("Comment was created but could not be read back.");
    }

    private async Task NotifyInterestedPartiesAsync(
        ZigZag.Application.Features.Tasks.Common.TaskDto task, Guid commenterId, CancellationToken cancellationToken)
    {
        var recipients = new HashSet<Guid>();
        if (task.AssignedToUserId is { } assignee)
        {
            recipients.Add(assignee);
        }
        recipients.Add(task.CreatedByUserId);
        recipients.Remove(commenterId); // never notify yourself

        foreach (var recipientId in recipients)
        {
            await _notificationPublisher.PublishAsync(new NotificationMessage(
                recipientId, "COMMENT_ADDED", $"New comment on \"{task.Title}\"", null, task.Id, task.ProjectId),
                cancellationToken);
        }
    }
}
