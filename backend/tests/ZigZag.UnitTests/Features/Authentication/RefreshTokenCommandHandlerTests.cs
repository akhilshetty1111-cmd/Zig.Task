using NSubstitute;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Authentication.Commands.RefreshToken;
using ZigZag.Application.Features.Authentication.Common;
using ZigZag.Domain.Entities;
using ZigZag.Domain.Enums;

namespace ZigZag.UnitTests.Features.Authentication;

/// <summary>
/// Locks in the rotation/reuse-detection behavior already proven correct
/// against the real database - see docs/architecture.md for the live
/// verification this pins as a regression test.
/// </summary>
public class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();

    private RefreshTokenCommandHandler CreateHandler() => new(_refreshTokenRepository, _userRepository, _tokenService);

    private static User ActiveUser() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Alice",
        Email = "alice@zigzag.dev",
        PasswordHash = "hash",
        Role = UserRole.Member,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Handle_UnknownToken_ThrowsAndDoesNotTouchTheRepository()
    {
        _tokenService.HashRefreshToken("raw").Returns("hash");
        _refreshTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns((RefreshTokenRecord?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RefreshTokenCommand("raw"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
        await _refreshTokenRepository.DidNotReceive().RevokeAllActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RevokedByLogout_ThrowsWithoutRevokingTheFamily()
    {
        var userId = Guid.NewGuid();
        var record = new RefreshTokenRecord(
            Guid.NewGuid(), userId, "hash", DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow, ReplacedByTokenHash: null);
        _tokenService.HashRefreshToken("raw").Returns("hash");
        _refreshTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(record);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RefreshTokenCommand("raw"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthenticationFailedException>();
        ex.Which.Message.Should().Be("This refresh token has been revoked.");
        await _refreshTokenRepository.DidNotReceive().RevokeAllActiveForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReusedRotatedToken_RevokesEntireFamily_TheReuseDetectionSecurityBehavior()
    {
        var userId = Guid.NewGuid();
        var record = new RefreshTokenRecord(
            Guid.NewGuid(), userId, "hash", DateTimeOffset.UtcNow.AddDays(1),
            RevokedAt: DateTimeOffset.UtcNow, ReplacedByTokenHash: "some-newer-hash");
        _tokenService.HashRefreshToken("raw").Returns("hash");
        _refreshTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(record);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RefreshTokenCommand("raw"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthenticationFailedException>();
        ex.Which.Message.Should().Contain("already been used").And.Contain("revoked");
        await _refreshTokenRepository.Received(1).RevokeAllActiveForUserAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExpiredToken_Throws()
    {
        var record = new RefreshTokenRecord(
            Guid.NewGuid(), Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(-1), RevokedAt: null, ReplacedByTokenHash: null);
        _tokenService.HashRefreshToken("raw").Returns("hash");
        _refreshTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(record);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RefreshTokenCommand("raw"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthenticationFailedException>();
        ex.Which.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task Handle_ValidToken_RevokesOldBeforePersistingNew_InThatOrder()
    {
        var user = ActiveUser();
        var record = new RefreshTokenRecord(
            Guid.NewGuid(), user.Id, "old-hash", DateTimeOffset.UtcNow.AddDays(1), RevokedAt: null, ReplacedByTokenHash: null);

        _tokenService.HashRefreshToken("raw").Returns("old-hash");
        _refreshTokenRepository.GetByTokenHashAsync("old-hash", Arg.Any<CancellationToken>()).Returns(record);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _tokenService.CreateAccessToken(user).Returns(new AccessToken("new-jwt", DateTimeOffset.UtcNow.AddMinutes(15)));
        _tokenService.GenerateRefreshToken().Returns("new-raw-token");
        _tokenService.HashRefreshToken("new-raw-token").Returns("new-hash");
        _tokenService.RefreshTokenLifetime.Returns(TimeSpan.FromDays(7));

        var handler = CreateHandler();
        var result = await handler.Handle(new RefreshTokenCommand("raw"), CancellationToken.None);

        result.AccessToken.Should().Be("new-jwt");
        result.RawRefreshToken.Should().Be("new-raw-token");

        // The old token is linked to the new one via ReplacedByTokenHash, and
        // revoked BEFORE the new row is persisted - the safer failure mode if
        // the process crashes mid-rotation (see RefreshTokenCommandHandler).
        Received.InOrder(() =>
        {
            _refreshTokenRepository.RevokeAsync("old-hash", "new-hash", Arg.Any<CancellationToken>());
            _refreshTokenRepository.AddAsync(user.Id, "new-hash", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_UserNoLongerActive_Throws()
    {
        var user = ActiveUser();
        user.IsActive = false;
        var record = new RefreshTokenRecord(
            Guid.NewGuid(), user.Id, "hash", DateTimeOffset.UtcNow.AddDays(1), RevokedAt: null, ReplacedByTokenHash: null);
        _tokenService.HashRefreshToken("raw").Returns("hash");
        _refreshTokenRepository.GetByTokenHashAsync("hash", Arg.Any<CancellationToken>()).Returns(record);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = CreateHandler();

        var act = () => handler.Handle(new RefreshTokenCommand("raw"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }
}
