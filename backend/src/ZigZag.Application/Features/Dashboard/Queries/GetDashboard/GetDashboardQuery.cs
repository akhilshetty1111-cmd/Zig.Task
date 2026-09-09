using MediatR;
using ZigZag.Application.Features.Dashboard.Common;

namespace ZigZag.Application.Features.Dashboard.Queries.GetDashboard;

public sealed record GetDashboardQuery : IRequest<DashboardDto>;
