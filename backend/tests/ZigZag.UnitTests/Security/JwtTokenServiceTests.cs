using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using ZigZag.Domain.Entities;
using ZigZag.Domain.Enums;
using ZigZag.Infrastructure.Security;

namespace ZigZag.UnitTests.Security;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(int accessMinutes = 15, int refreshDays = 7)
        => new(Options.Create(new JwtSettings
        {
            // 32+ bytes, matching the minimum HMAC-SHA256 needs - real tests,
            // not a trivially short throwaway key.
            Key = "unit-test-signing-key-unit-test-signing-key-32bytes+",
            Issuer = "https://zigzag.api.tests",
            Audience = "https://zigzag.web.tests",
            AccessTokenMinutes = accessMinutes,
            RefreshTokenDays = refreshDays,
        }));

    private static User TestUser() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Alice Example",
        Email = "alice@zigzag.dev",
        PasswordHash = "irrelevant",
        Role = UserRole.ProjectManager,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void CreateAccessToken_ProducesATokenWithTheExpectedClaims()
    {
        var service = CreateService();
        var user = TestUser();

        var token = service.CreateAccessToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);
        jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value.Should().Be(user.Id.ToString());
        jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value.Should().Be(user.Email);
        jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value.Should().Be(user.Name);
        jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value.Should().Be("ProjectManager");
        jwt.Issuer.Should().Be("https://zigzag.api.tests");
        jwt.Audiences.Should().Contain("https://zigzag.web.tests");
    }

    [Fact]
    public void CreateAccessToken_ExpiresAtMatchesConfiguredMinutes()
    {
        var service = CreateService(accessMinutes: 20);
        var before = DateTimeOffset.UtcNow;

        var token = service.CreateAccessToken(TestUser());

        token.ExpiresAt.Should().BeCloseTo(before.AddMinutes(20), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CreateAccessToken_TwoTokensForTheSameUser_HaveDifferentJtiClaims()
    {
        var service = CreateService();
        var user = TestUser();

        var token1 = new JwtSecurityTokenHandler().ReadJwtToken(service.CreateAccessToken(user).Value);
        var token2 = new JwtSecurityTokenHandler().ReadJwtToken(service.CreateAccessToken(user).Value);

        token1.Id.Should().NotBe(token2.Id);
    }

    [Fact]
    public void RefreshTokenLifetime_MatchesConfiguredDays()
    {
        var service = CreateService(refreshDays: 10);

        service.RefreshTokenLifetime.Should().Be(TimeSpan.FromDays(10));
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueHighEntropyValues()
    {
        var service = CreateService();

        var token1 = service.GenerateRefreshToken();
        var token2 = service.GenerateRefreshToken();

        token1.Should().NotBe(token2);
        token1.Length.Should().BeGreaterThan(32);
    }

    [Fact]
    public void HashRefreshToken_IsDeterministic_SoALookupByHashWorks()
    {
        var service = CreateService();

        service.HashRefreshToken("same-raw-token").Should().Be(service.HashRefreshToken("same-raw-token"));
    }

    [Fact]
    public void HashRefreshToken_DifferentInputs_ProduceDifferentHashes()
    {
        var service = CreateService();

        service.HashRefreshToken("token-a").Should().NotBe(service.HashRefreshToken("token-b"));
    }
}
