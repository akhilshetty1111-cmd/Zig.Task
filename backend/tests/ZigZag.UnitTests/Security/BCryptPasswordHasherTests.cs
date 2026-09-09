using ZigZag.Infrastructure.Security;

namespace ZigZag.UnitTests.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_NeverReturnsThePlaintext()
    {
        var hash = _hasher.Hash("Passw0rd!");

        hash.Should().NotBe("Passw0rd!");
        hash.Should().StartWith("$2");
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Passw0rd!");

        _hasher.Verify("Passw0rd!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Passw0rd!");

        _hasher.Verify("SomethingElse1", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        // BCrypt salts each hash independently - two hashes of the same
        // password must never be equal, or a leaked database would let an
        // attacker spot which accounts share a password.
        var hash1 = _hasher.Hash("Passw0rd!");
        var hash2 = _hasher.Hash("Passw0rd!");

        hash1.Should().NotBe(hash2);
        _hasher.Verify("Passw0rd!", hash1).Should().BeTrue();
        _hasher.Verify("Passw0rd!", hash2).Should().BeTrue();
    }
}
