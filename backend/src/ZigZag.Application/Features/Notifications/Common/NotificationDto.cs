namespace ZigZag.Application.Features.Notifications.Common;

public sealed record NotificationDto(
    Guid Id, string Type, string Title, string? Message, Guid? TaskId, Guid? ProjectId, bool IsRead, DateTimeOffset CreatedAt);
