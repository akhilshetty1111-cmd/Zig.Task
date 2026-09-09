using ZigZag.Domain.Entities;

namespace ZigZag.Application.Common.Interfaces;

/// <summary>
/// User persistence. Lives in Common (not scoped to Features/Authentication)
/// because Projects, Tasks and Comments all need to look up users too -
/// e.g. resolving an assignee or a project member.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <returns>The new user's id.</returns>
    Task<Guid> CreateAsync(User user, CancellationToken cancellationToken = default);
}
