using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZigZag.Application.Features.Dashboard.Common;
using ZigZag.Application.Features.Dashboard.Queries.GetDashboard;

namespace ZigZag.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Every dashboard metric in one call - see IDashboardRepository for why
    /// this is a single batched query rather than several separate ones.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetDashboardQuery(), cancellationToken));
}
