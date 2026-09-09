using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Projects.Commands.RemoveProjectMember;

public sealed class RemoveProjectMemberCommandHandler : IRequestHandler<RemoveProjectMemberCommand>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProjectAuthorizationService _authorization;

    public RemoveProjectMemberCommandHandler(
        IProjectRepository projectRepository, ICurrentUserService currentUserService, ProjectAuthorizationService authorization)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
        _authorization = authorization;
    }

    public async Task Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var callerId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");
        await _authorization.EnsureRoleAsync(request.ProjectId, callerId, ProjectRole.Manager, cancellationToken);

        var targetRole = await _projectRepository.GetMemberRoleAsync(request.ProjectId, request.UserId, cancellationToken)
            ?? throw new NotFoundException("This user is not a member of the project.");

        if (targetRole == ProjectRole.Owner)
        {
            var ownerCount = await _projectRepository.CountOwnersAsync(request.ProjectId, cancellationToken);
            if (ownerCount <= 1)
            {
                throw new ConflictException("Cannot remove the last Owner of a project.");
            }
        }

        await _projectRepository.RemoveMemberAsync(request.ProjectId, request.UserId, cancellationToken);
    }
}
