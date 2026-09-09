using NSubstitute;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Commands.RemoveProjectMember;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.UnitTests.Features.Projects;

/// <summary>Locks in the last-Owner guard already proven correct live - see docs/architecture.md.</summary>
public class RemoveProjectMemberCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private RemoveProjectMemberCommandHandler CreateHandler()
        => new(_projectRepository, _currentUserService, new ProjectAuthorizationService(_projectRepository));

    [Fact]
    public async Task Handle_RemovingTheOnlyOwner_ThrowsConflict_AndDoesNotRemove()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _currentUserService.UserId.Returns(callerId);
        // Caller is a Manager (sufficient to attempt removal); target is the sole Owner.
        _projectRepository.GetMemberRoleAsync(projectId, callerId, Arg.Any<CancellationToken>()).Returns(ProjectRole.Manager);
        _projectRepository.GetMemberRoleAsync(projectId, targetId, Arg.Any<CancellationToken>()).Returns(ProjectRole.Owner);
        _projectRepository.CountOwnersAsync(projectId, Arg.Any<CancellationToken>()).Returns(1);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RemoveProjectMemberCommand(projectId, targetId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _projectRepository.DidNotReceive().RemoveMemberAsync(projectId, targetId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RemovingOneOfSeveralOwners_Succeeds()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _currentUserService.UserId.Returns(callerId);
        _projectRepository.GetMemberRoleAsync(projectId, callerId, Arg.Any<CancellationToken>()).Returns(ProjectRole.Owner);
        _projectRepository.GetMemberRoleAsync(projectId, targetId, Arg.Any<CancellationToken>()).Returns(ProjectRole.Owner);
        _projectRepository.CountOwnersAsync(projectId, Arg.Any<CancellationToken>()).Returns(2);
        var handler = CreateHandler();

        await handler.Handle(new RemoveProjectMemberCommand(projectId, targetId), CancellationToken.None);

        await _projectRepository.Received(1).RemoveMemberAsync(projectId, targetId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RemovingANonMember_ThrowsNotFound()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        _currentUserService.UserId.Returns(callerId);
        _projectRepository.GetMemberRoleAsync(projectId, callerId, Arg.Any<CancellationToken>()).Returns(ProjectRole.Owner);
        _projectRepository.GetMemberRoleAsync(projectId, targetId, Arg.Any<CancellationToken>()).Returns((ProjectRole?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RemoveProjectMemberCommand(projectId, targetId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
