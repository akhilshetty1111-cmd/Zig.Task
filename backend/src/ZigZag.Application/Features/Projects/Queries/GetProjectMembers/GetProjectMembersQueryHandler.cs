using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;

namespace ZigZag.Application.Features.Projects.Queries.GetProjectMembers;

/// <summary>Any member may view the member list - viewing does not require Manager/Owner.</summary>
public sealed class GetProjectMembersQueryHandler : IRequestHandler<GetProjectMembersQuery, IReadOnlyList<ProjectMemberDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetProjectMembersQueryHandler(IProjectRepository projectRepository, ICurrentUserService currentUserService)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ProjectMemberDto>> Handle(
        GetProjectMembersQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");

        var role = await _projectRepository.GetMemberRoleAsync(request.ProjectId, userId, cancellationToken);
        if (role is null)
        {
            throw new NotFoundException("Project", request.ProjectId);
        }

        return await _projectRepository.GetMembersAsync(request.ProjectId, cancellationToken);
    }
}
