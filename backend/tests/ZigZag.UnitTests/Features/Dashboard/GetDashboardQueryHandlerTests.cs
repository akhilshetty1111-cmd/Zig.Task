using NSubstitute;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Dashboard.Common;
using ZigZag.Application.Features.Dashboard.Queries.GetDashboard;

namespace ZigZag.UnitTests.Features.Dashboard;

public class GetDashboardQueryHandlerTests
{
    private readonly IDashboardRepository _dashboardRepository = Substitute.For<IDashboardRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private GetDashboardQueryHandler CreateHandler() => new(_dashboardRepository, _currentUserService);

    [Fact]
    public async Task Handle_FillsInZeroForEveryStatusAndPriorityNotReturnedBySql()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);
        // The repository only returns rows for categories that actually have
        // tasks - here, just Todo and Medium.
        _dashboardRepository.GetForUserAsync(userId, Arg.Any<CancellationToken>()).Returns(new DashboardDto(
            TotalProjects: 1, TotalTasks: 1, CompletedTasks: 0, PendingTasks: 1, OverdueTasks: 0, TasksAssignedToMe: 0,
            TasksByStatus: [new StatusCountDto("Todo", 1)],
            TasksByPriority: [new PriorityCountDto("Medium", 1)],
            RecentActivities: []));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        result.TasksByStatus.Should().HaveCount(5); // Todo, InProgress, InReview, Done, Blocked
        result.TasksByStatus.Should().ContainSingle(s => s.Status == "Todo" && s.Count == 1);
        result.TasksByStatus.Where(s => s.Status != "Todo").Should().OnlyContain(s => s.Count == 0);

        result.TasksByPriority.Should().HaveCount(4); // Low, Medium, High, Urgent
        result.TasksByPriority.Should().ContainSingle(p => p.Priority == "Medium" && p.Count == 1);
        result.TasksByPriority.Where(p => p.Priority != "Medium").Should().OnlyContain(p => p.Count == 0);
    }

    [Fact]
    public async Task Handle_PassesThroughScalarMetricsUnchanged()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);
        _dashboardRepository.GetForUserAsync(userId, Arg.Any<CancellationToken>()).Returns(new DashboardDto(
            TotalProjects: 3, TotalTasks: 10, CompletedTasks: 4, PendingTasks: 6, OverdueTasks: 2, TasksAssignedToMe: 5,
            TasksByStatus: [], TasksByPriority: [], RecentActivities: []));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        result.TotalProjects.Should().Be(3);
        result.TotalTasks.Should().Be(10);
        result.CompletedTasks.Should().Be(4);
        result.PendingTasks.Should().Be(6);
        result.OverdueTasks.Should().Be(2);
        result.TasksAssignedToMe.Should().Be(5);
    }
}
