using ZigZag.Application.Features.Notifications.Common;
using ZigZag.Domain.Entities;

namespace ZigZag.Application.Common.Interfaces;

/// <summary>
/// Lives in Common, not Features/Notifications: Tasks (assignment) and
/// Comments (new comment) both create notifications as a side effect of
/// their own action.
/// </summary>
public interface INotificationRepository
{
    Task<Guid> CreateAsync(Notification notification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetForUserAsync(
        Guid userId, bool unreadOnly, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
}
