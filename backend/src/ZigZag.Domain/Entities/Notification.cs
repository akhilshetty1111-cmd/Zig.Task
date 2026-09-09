namespace ZigZag.Domain.Entities;

/// <summary>An in-app alert for a user. Maps to the <c>notifications</c> table.</summary>
public sealed class Notification
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string Type { get; init; }
    public required string Title { get; init; }
    public string? Message { get; init; }
    public Guid? TaskId { get; init; }
    public Guid? ProjectId { get; init; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
