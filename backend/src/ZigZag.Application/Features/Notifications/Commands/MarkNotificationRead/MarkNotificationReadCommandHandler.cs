using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;

namespace ZigZag.Application.Features.Notifications.Commands.MarkNotificationRead;

/// <summary>
/// The UPDATE is scoped to (id, user_id) at the SQL level - marking someone
/// else's notification as read simply matches zero rows rather than needing
/// a separate ownership check and a 403/404 for what is a low-stakes action.
/// </summary>
public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public MarkNotificationReadCommandHandler(
        INotificationRepository notificationRepository, ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");
        return _notificationRepository.MarkAsReadAsync(request.NotificationId, userId, cancellationToken);
    }
}
