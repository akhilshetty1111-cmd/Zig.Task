namespace ZigZag.Application.Common.Interfaces;

/// <summary>
/// Hashes and verifies passwords. Implemented with BCrypt in Infrastructure -
/// Application only depends on the abstraction, never a hashing library
/// directly, so the algorithm can change without touching handler code.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
