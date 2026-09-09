using MediatR;
using ZigZag.Application.Features.Notifications.Common;

namespace ZigZag.Application.Features.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery(bool UnreadOnly) : IRequest<IReadOnlyList<NotificationDto>>;
