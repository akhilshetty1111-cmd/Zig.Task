using NSubstitute;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.UnitTests.Features.Projects;

public class ProjectAuthorizationServiceTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private ProjectAuthorizationService CreateService() => new(_projectRepository);

    [Fact]
    public async Task EnsureRoleAsync_NonMember_ThrowsNotFound_NotForbidden()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _projectRepository.GetMemberRoleAsync(projectId, userId, Arg.Any<CancellationToken>()).Returns((ProjectRole?)null);

        var act = () => CreateService().EnsureRoleAsync(projectId, userId, ProjectRole.Member, CancellationToken.None);

        // NotFound, not Forbidden - a non-member should not learn the project exists.
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData(ProjectRole.Viewer, ProjectRole.Member, false)]
    [InlineData(ProjectRole.Member, ProjectRole.Manager, false)]
    [InlineData(ProjectRole.Manager, ProjectRole.Manager, true)]
    [InlineData(ProjectRole.Owner, ProjectRole.Manager, true)]
    [InlineData(ProjectRole.Owner, ProjectRole.Owner, true)]
    public async Task EnsureRoleAsync_ComparesActualRoleAgainstMinimum(
        ProjectRole actualRole, ProjectRole minimumRole, bool shouldSucceed)
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _projectRepository.GetMemberRoleAsync(projectId, userId, Arg.Any<CancellationToken>()).Returns(actualRole);
        var service = CreateService();

        var act = () => service.EnsureRoleAsync(projectId, userId, minimumRole, CancellationToken.None);

        if (shouldSucceed)
        {
            var result = await act();
            result.Should().Be(actualRole);
        }
        else
        {
            await act.Should().ThrowAsync<ForbiddenAccessException>();
        }
    }
}
