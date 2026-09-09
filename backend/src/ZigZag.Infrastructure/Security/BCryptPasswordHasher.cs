using ZigZag.Application.Common.Interfaces;

namespace ZigZag.Infrastructure.Security;

/// <summary>
/// Work factor 11 matches the hash already committed in database/seeds/dev_seed.sql -
/// keep them in sync if this changes, or the seeded dev accounts stop verifying.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
