namespace ZigZag.Domain.Enums;

/// <summary>
/// System-wide role, carried in the JWT "role" claim and used by
/// [Authorize(Roles = ...)] on controllers.
/// </summary>
/// <remarks>
/// Distinct from <see cref="ProjectRole"/>: this governs what a user may do across
/// the whole installation, whereas ProjectRole governs a single project membership.
/// </remarks>
public enum UserRole
{
    Member = 0,
    ProjectManager = 1,
    Admin = 2
}
