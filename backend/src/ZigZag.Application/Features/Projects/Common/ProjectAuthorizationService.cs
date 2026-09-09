using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Projects.Common;

/// <summary>
/// The membership/role check every project-scoped command needs before it
/// touches anything. Shared rather than duplicated per handler.
/// </summary>
/// <remarks>
/// A non-member gets NotFoundException (404), not Forbidden (403) - they
/// should not learn the project exists at all. A member whose role is too
/// low gets Forbidden - they already know the project exists, so there is
/// nothing left to hide. ProjectRole's ordinal order (Viewer &lt; Member &lt;
/// Manager &lt; Owner, see the enum) is what makes the plain <c>&lt;</c>
/// comparison below correct.
/// </remarks>
public sealed class ProjectAuthorizationService
{
    private readonly IProjectRepository _projectRepository;

    public ProjectAuthorizationService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    /// <returns>The caller's actual role, for handlers that need it beyond the pass/fail check.</returns>
    public async Task<ProjectRole> EnsureRoleAsync(
        Guid projectId, Guid userId, ProjectRole minimumRole, CancellationToken cancellationToken)
    {
        var role = await _projectRepository.GetMemberRoleAsync(projectId, userId, cancellationToken);

        if (role is null)
        {
            throw new NotFoundException("Project", projectId);
        }

        if (role < minimumRole)
        {
            throw new ForbiddenAccessException(
                $"This action requires the {minimumRole} role or higher on this project.");
        }

        return role.Value;
    }
}
