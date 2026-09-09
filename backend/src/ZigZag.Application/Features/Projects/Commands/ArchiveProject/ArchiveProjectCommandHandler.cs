using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Projects.Commands.ArchiveProject;

/// <summary>
/// Archives (soft-deletes) a project - Owner only. A Manager can run the
/// project day-to-day but taking it out of active use entirely is reserved
/// for the Owner; see docs/architecture.md for why DELETE archives rather
/// than hard-deleting.
/// </summary>
public sealed class ArchiveProjectCommandHandler : IRequestHandler<ArchiveProjectCommand>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProjectAuthorizationService _authorization;

    public ArchiveProjectCommandHandler(
        IProjectRepository projectRepository, ICurrentUserService currentUserService, ProjectAuthorizationService authorization)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
        _authorization = authorization;
    }

    public async Task Handle(ArchiveProjectCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");

        await _authorization.EnsureRoleAsync(request.ProjectId, userId, ProjectRole.Owner, cancellationToken);

        await _projectRepository.ArchiveAsync(request.ProjectId, cancellationToken);
    }
}
