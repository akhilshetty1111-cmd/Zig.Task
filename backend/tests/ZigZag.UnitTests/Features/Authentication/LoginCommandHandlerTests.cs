using NSubstitute;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Authentication.Commands.Login;
using ZigZag.Application.Features.Authentication.Common;
using ZigZag.Domain.Entities;
using ZigZag.Domain.Enums;

namespace ZigZag.UnitTests.Features.Authentication;

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();

    private LoginCommandHandler CreateHandler()
        => new(_userRepository, _passwordHasher, new AuthTokenIssuer(_tokenService, _refreshTokenRepository));

    private static User ActiveUser(string email = "alice@zigzag.dev", string hash = "hashed") => new()
    {
        Id = Guid.NewGuid(),
        Name = "Alice",
        Email = email,
        PasswordHash = hash,
        Role = UserRole.Member,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Handle_UnknownEmail_ThrowsWithGenericMessage()
    {
        _userRepository.GetByEmailAsync("nobody@zigzag.dev", Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new LoginCommand("nobody@zigzag.dev", "whatever"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthenticationFailedException>();
        ex.Which.Message.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsWithTheSameGenericMessageAsUnknownEmail()
    {
        var user = ActiveUser();
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong", user.PasswordHash).Returns(false);
        var handler = CreateHandler();

        var act = () => handler.Handle(new LoginCommand(user.Email, "wrong"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AuthenticationFailedException>();
        // Must match the unknown-email message exactly - a different message
        // per case would let a caller enumerate registered emails.
        ex.Which.Message.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_InactiveAccount_RejectsEvenWithCorrectPassword()
    {
        var user = ActiveUser();
        user.IsActive = false;
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("correct", user.PasswordHash).Returns(true);
        var handler = CreateHandler();

        var act = () => handler.Handle(new LoginCommand(user.Email, "correct"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Handle_CorrectCredentials_IssuesTokensAndPersistsRefreshToken()
    {
        var user = ActiveUser();
        _userRepository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("correct", user.PasswordHash).Returns(true);
        _tokenService.CreateAccessToken(user).Returns(new AccessToken("jwt-value", DateTimeOffset.UtcNow.AddMinutes(15)));
        _tokenService.GenerateRefreshToken().Returns("raw-refresh-token");
        _tokenService.HashRefreshToken("raw-refresh-token").Returns("hashed-refresh-token");
        _tokenService.RefreshTokenLifetime.Returns(TimeSpan.FromDays(7));
        var handler = CreateHandler();

        var result = await handler.Handle(new LoginCommand(user.Email, "correct"), CancellationToken.None);

        result.AccessToken.Should().Be("jwt-value");
        result.RawRefreshToken.Should().Be("raw-refresh-token");
        result.User.Email.Should().Be(user.Email);
        await _refreshTokenRepository.Received(1).AddAsync(
            user.Id, "hashed-refresh-token", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
