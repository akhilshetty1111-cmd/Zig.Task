namespace ZigZag.Application.Features.Attachments.Common;

public sealed record AttachmentDto(
    Guid Id,
    Guid TaskId,
    Guid UploadedBy,
    string UploadedByName,
    string FileName,
    string FileUrl,
    long FileSizeBytes,
    string ContentType,
    DateTimeOffset CreatedAt);
