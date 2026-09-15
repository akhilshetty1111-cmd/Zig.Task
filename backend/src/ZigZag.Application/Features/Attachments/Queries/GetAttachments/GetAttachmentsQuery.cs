using MediatR;
using ZigZag.Application.Features.Attachments.Common;

namespace ZigZag.Application.Features.Attachments.Queries.GetAttachments;

public sealed record GetAttachmentsQuery(Guid TaskId) : IRequest<IReadOnlyList<AttachmentDto>>;
