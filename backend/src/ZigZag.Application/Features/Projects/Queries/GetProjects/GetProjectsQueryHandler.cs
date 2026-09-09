using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;

namespace ZigZag.Application.Features.Projects.Queries.GetProjects;

public sealed class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, IReadOnlyList<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetProjectsQueryHandler(IProjectRepository projectRepository, ICurrentUserService currentUserService)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
    }

    public Task<IReadOnlyList<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");
        return _projectRepository.GetForUserAsync(userId, request.IncludeArchived, cancellationToken);
    }
}
