using ZigZag.Application.Features.Dashboard.Common;

namespace ZigZag.Application.Common.Interfaces;

/// <summary>
/// One call, one round trip: per the "optimized dashboard query rather than
/// many unnecessary API calls" requirement, the implementation runs every
/// metric as a single batched Dapper QueryMultipleAsync rather than the
/// handler making several separate repository calls.
/// </summary>
public interface IDashboardRepository
{
    Task<DashboardDto> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
