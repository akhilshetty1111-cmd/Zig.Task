namespace ZigZag.Domain.Entities;

/// <summary>A top-level container for tasks. Maps to the <c>projects</c> table.</summary>
public sealed class Project
{
    public required Guid Id { get; init; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required Guid OwnerId { get; init; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
