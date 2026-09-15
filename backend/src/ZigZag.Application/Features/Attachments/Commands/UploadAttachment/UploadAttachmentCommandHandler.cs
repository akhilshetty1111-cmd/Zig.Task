using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Attachments.Common;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Entities;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Attachments.Commands.UploadAttachment;

public sealed class UploadAttachmentCommandHandler : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
{
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProjectAuthorizationService _authorization;

    public UploadAttachmentCommandHandler(
        IAttachmentRepository attachmentRepository,
        ITaskRepository taskRepository,
        IBlobStorageService blobStorageService,
        ICurrentUserService currentUserService,
        ProjectAuthorizationService authorization)
    {
        _attachmentRepository = attachmentRepository;
        _taskRepository = taskRepository;
        _blobStorageService = blobStorageService;
        _currentUserService = currentUserService;
        _authorization = authorization;
    }

    public async Task<AttachmentDto> Handle(UploadAttachmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException("Task", request.TaskId);

        await _authorization.EnsureRoleAsync(task.ProjectId, userId, ProjectRole.Member, cancellationToken);

        // Guid-prefixed blob name: two people attaching a file with the same
        // original name to the same task must never collide.
        var blobName = $"{request.TaskId}/{Guid.NewGuid()}-{request.FileName}";
        var fileUrl = await _blobStorageService.UploadAsync(
            request.Content, blobName, request.ContentType, cancellationToken);

        var attachment = new TaskAttachment
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            UploadedBy = userId,
            FileName = request.FileName,
            FileUrl = fileUrl,
            FileSizeBytes = request.FileSizeBytes,
            ContentType = request.ContentType,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await _attachmentRepository.CreateAsync(attachment, cancellationToken);

        return await _attachmentRepository.GetByIdAsync(attachment.Id, cancellationToken)
            ?? throw new InvalidOperationException("Attachment was created but could not be read back.");
    }
}
