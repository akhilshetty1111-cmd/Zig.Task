using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Dashboard.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Dashboard.Queries.GetDashboard;

/// <summary>
/// Fills in a zero count for every status/priority the repository's GROUP BY
/// did not return a row for (a user with no BLOCKED tasks gets no BLOCKED row
/// from SQL) - a dashboard chart should show every category, not silently
/// omit the ones currently at zero.
/// </summary>
public sealed class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetDashboardQueryHandler(IDashboardRepository dashboardRepository, ICurrentUserService currentUserService)
    {
        _dashboardRepository = dashboardRepository;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");

        var dashboard = await _dashboardRepository.GetForUserAsync(userId, cancellationToken);

        var statusCounts = Enum.GetValues<TaskItemStatus>()
            .Select(status => dashboard.TasksByStatus.FirstOrDefault(s => s.Status == status.ToString())
                ?? new StatusCountDto(status.ToString(), 0))
            .ToList();

        var priorityCounts = Enum.GetValues<TaskPriority>()
            .Select(priority => dashboard.TasksByPriority.FirstOrDefault(p => p.Priority == priority.ToString())
                ?? new PriorityCountDto(priority.ToString(), 0))
            .ToList();

        return dashboard with { TasksByStatus = statusCounts, TasksByPriority = priorityCounts };
    }
}
