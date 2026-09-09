using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Entities;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Common.Interfaces;

/// <summary>
/// Project and project-membership persistence. Lives in Common (not scoped to
/// Features/Projects) because Tasks, Comments and the Dashboard all need the
/// membership/role checks - GetMemberRoleAsync is the building block every
/// project-scoped authorization check is built on from Phase 6 onward.
/// </summary>
public interface IProjectRepository
{
    Task<ProjectDto?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectDto>> GetForUserAsync(
        Guid userId, bool includeArchived, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(Project project, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid projectId, string name, string? description, CancellationToken cancellationToken = default);

    Task ArchiveAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>The caller's role on this project, or null if they are not a member.</summary>
    Task<ProjectRole?> GetMemberRoleAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectMemberDto>> GetMembersAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task AddMemberAsync(Guid projectId, Guid userId, ProjectRole role, CancellationToken cancellationToken = default);

    Task RemoveMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Used to stop the last Owner being removed and leaving a project ownerless.</summary>
    Task<int> CountOwnersAsync(Guid projectId, CancellationToken cancellationToken = default);
}
