using ZigZag.Domain.Enums;

namespace ZigZag.Domain.Entities;

/// <summary>
/// An application account. Maps to the <c>users</c> table.
/// </summary>
public sealed class User
{
    public required Guid Id { get; init; }
    public required string Name { get; set; }
    public required string Email { get; set; }

    /// <summary>BCrypt hash - never the plaintext password.</summary>
    public required string PasswordHash { get; set; }

    public required UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
