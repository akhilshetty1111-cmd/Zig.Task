using ZigZag.Domain.Enums;

namespace ZigZag.Domain.Entities;

/// <summary>
/// A unit of work within a project. Maps to the <c>tasks</c> table. Named
/// TaskItem, not Task - see docs/architecture.md, decision 7.
/// </summary>
public sealed class TaskItem
{
    public required Guid Id { get; init; }
    public required Guid ProjectId { get; init; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public Guid? AssignedTo { get; set; }
    public required Guid CreatedBy { get; init; }
    public DateOnly? DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
