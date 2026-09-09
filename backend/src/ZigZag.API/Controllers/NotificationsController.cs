using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZigZag.Application.Features.Notifications.Commands.MarkNotificationRead;
using ZigZag.Application.Features.Notifications.Common;
using ZigZag.Application.Features.Notifications.Queries.GetNotifications;

namespace ZigZag.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetNotifications(
        [FromQuery] bool unreadOnly, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetNotificationsQuery(unreadOnly), cancellationToken));

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new MarkNotificationReadCommand(id), cancellationToken);
        return NoContent();
    }
}
