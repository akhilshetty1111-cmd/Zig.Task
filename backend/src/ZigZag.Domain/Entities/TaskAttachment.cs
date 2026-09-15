namespace ZigZag.Domain.Entities;

public sealed class TaskAttachment
{
    public required Guid Id { get; init; }
    public required Guid TaskId { get; init; }
    public required Guid UploadedBy { get; init; }
    public required string FileName { get; init; }
    public required string FileUrl { get; init; }
    public required long FileSizeBytes { get; init; }
    public required string ContentType { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
