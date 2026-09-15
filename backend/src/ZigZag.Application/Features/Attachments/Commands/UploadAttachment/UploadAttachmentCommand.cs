using MediatR;
using ZigZag.Application.Features.Attachments.Common;

namespace ZigZag.Application.Features.Attachments.Commands.UploadAttachment;

public sealed record UploadAttachmentCommand(
    Guid TaskId, string FileName, string ContentType, long FileSizeBytes, Stream Content) : IRequest<AttachmentDto>;
