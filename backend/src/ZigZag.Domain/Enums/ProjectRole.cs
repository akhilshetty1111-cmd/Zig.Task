namespace ZigZag.Domain.Enums;

/// <summary>
/// A user's role within one specific project, stored on project_members.
/// Authorization for project-scoped actions is resolved from this, not from
/// <see cref="UserRole"/> - an Admin is not automatically a member of every project.
/// </summary>
public enum ProjectRole
{
    Viewer = 0,
    Member = 1,
    Manager = 2,
    Owner = 3
}
