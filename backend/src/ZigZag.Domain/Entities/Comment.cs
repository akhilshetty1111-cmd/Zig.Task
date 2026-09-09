namespace ZigZag.Domain.Entities;

/// <summary>A remark on a task. Maps to the <c>task_comments</c> table.</summary>
public sealed class Comment
{
    public required Guid Id { get; init; }
    public required Guid TaskId { get; init; }
    public required Guid UserId { get; init; }
    public required string Text { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
