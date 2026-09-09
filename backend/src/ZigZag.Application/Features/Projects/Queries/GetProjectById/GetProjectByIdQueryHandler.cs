using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;

namespace ZigZag.Application.Features.Projects.Queries.GetProjectById;

/// <summary>Non-members get NotFoundException, not Forbidden - see ProjectAuthorizationService's remarks.</summary>
public sealed class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetProjectByIdQueryHandler(IProjectRepository projectRepository, ICurrentUserService currentUserService)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ProjectDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");

        var role = await _projectRepository.GetMemberRoleAsync(request.ProjectId, userId, cancellationToken);
        if (role is null)
        {
            throw new NotFoundException("Project", request.ProjectId);
        }

        return await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);
    }
}
