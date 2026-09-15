using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Attachments.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Attachments.Commands.DeleteAttachment;

/// <summary>The uploader may delete their own attachment; so may a project Manager+ (moderation).</summary>
public sealed class DeleteAttachmentCommandHandler : IRequestHandler<DeleteAttachmentCommand>
{
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProjectRepository _projectRepository;

    public DeleteAttachmentCommandHandler(
        IAttachmentRepository attachmentRepository,
        ITaskRepository taskRepository,
        IBlobStorageService blobStorageService,
        ICurrentUserService currentUserService,
        IProjectRepository projectRepository)
    {
        _attachmentRepository = attachmentRepository;
        _taskRepository = taskRepository;
        _blobStorageService = blobStorageService;
        _currentUserService = currentUserService;
        _projectRepository = projectRepository;
    }

    public async Task Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");

        var attachment = await _attachmentRepository.GetByIdAsync(request.AttachmentId, cancellationToken)
            ?? throw new NotFoundException("Attachment", request.AttachmentId);

        if (attachment.UploadedBy != userId)
        {
            var task = await _taskRepository.GetByIdAsync(attachment.TaskId, cancellationToken)
                ?? throw new NotFoundException("Task", attachment.TaskId);
            var role = await _projectRepository.GetMemberRoleAsync(task.ProjectId, userId, cancellationToken);

            if (role is null or < ProjectRole.Manager)
            {
                throw new ForbiddenAccessException("You may only delete your own attachments.");
            }
        }

        await _blobStorageService.DeleteAsync(attachment.FileUrl, cancellationToken);
        await _attachmentRepository.DeleteAsync(request.AttachmentId, cancellationToken);
    }
}
