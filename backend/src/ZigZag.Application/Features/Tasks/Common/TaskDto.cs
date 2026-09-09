namespace ZigZag.Application.Features.Tasks.Common;

public sealed record TaskDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    string Status,
    string Priority,
    Guid? AssignedToUserId,
    string? AssignedToName,
    Guid CreatedByUserId,
    string CreatedByName,
    DateOnly? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt);

public sealed record TaskHistoryEntryDto(
    Guid Id, Guid ChangedByUserId, string ChangedByName, string FieldName, string? OldValue, string? NewValue, DateTimeOffset ChangedAt);
