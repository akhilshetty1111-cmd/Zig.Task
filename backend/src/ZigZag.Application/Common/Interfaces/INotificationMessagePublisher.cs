namespace ZigZag.Application.Common.Interfaces;

/// <summary>The data needed to create a notification row, in flight on a Service Bus queue.</summary>
public sealed record NotificationMessage(
    Guid UserId, string Type, string Title, string? Message, Guid? TaskId, Guid? ProjectId);

/// <summary>
/// Publishes a notification-worthy event for async processing instead of
/// writing to <see cref="INotificationRepository"/> inline. Implemented in
/// Infrastructure (Azure Service Bus) - Application never touches the Azure
/// SDK directly, same reasoning as <see cref="ITokenService"/> and
/// <see cref="IBlobStorageService"/>.
/// </summary>
public interface INotificationMessagePublisher
{
    Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
