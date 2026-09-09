using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Comments.Common;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Comments.Queries.GetComments;

public sealed class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, IReadOnlyList<CommentDto>>
{
    private readonly ICommentRepository _commentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProjectAuthorizationService _authorization;

    public GetCommentsQueryHandler(
        ICommentRepository commentRepository,
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        ProjectAuthorizationService authorization)
    {
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _authorization = authorization;
    }

    public async Task<IReadOnlyList<CommentDto>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        await _authorization.EnsureRoleAsync(task.ProjectId, userId, ProjectRole.Viewer, cancellationToken);

        return await _commentRepository.GetForTaskAsync(request.TaskId, cancellationToken);
    }
}
