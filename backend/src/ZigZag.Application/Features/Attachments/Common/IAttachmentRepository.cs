using ZigZag.Domain.Entities;

namespace ZigZag.Application.Features.Attachments.Common;

public interface IAttachmentRepository
{
    Task<AttachmentDto?> GetByIdAsync(Guid attachmentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttachmentDto>> GetForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(TaskAttachment attachment, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid attachmentId, CancellationToken cancellationToken = default);
}
