using MediatR;

namespace ZigZag.Application.Features.Attachments.Commands.DeleteAttachment;

public sealed record DeleteAttachmentCommand(Guid AttachmentId) : IRequest;
