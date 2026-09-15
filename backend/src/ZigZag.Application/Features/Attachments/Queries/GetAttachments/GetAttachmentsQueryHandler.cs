using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Attachments.Common;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Attachments.Queries.GetAttachments;

public sealed class GetAttachmentsQueryHandler : IRequestHandler<GetAttachmentsQuery, IReadOnlyList<AttachmentDto>>
{
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProjectAuthorizationService _authorization;

    public GetAttachmentsQueryHandler(
        IAttachmentRepository attachmentRepository,
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        ProjectAuthorizationService authorization)
    {
        _attachmentRepository = attachmentRepository;
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _authorization = authorization;
    }

    public async Task<IReadOnlyList<AttachmentDto>> Handle(GetAttachmentsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        await _authorization.EnsureRoleAsync(task.ProjectId, userId, ProjectRole.Viewer, cancellationToken);

        return await _attachmentRepository.GetForTaskAsync(request.TaskId, cancellationToken);
    }
}
