using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Authentication.Common;

namespace ZigZag.Application.Features.Authentication.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    // One message for every failure reason (unknown email, wrong password,
    // deactivated account) - a distinct message per case would let a caller
    // enumerate which emails are registered.
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AuthTokenIssuer _tokenIssuer;

    public LoginCommandHandler(
        IUserRepository userRepository, IPasswordHasher passwordHasher, AuthTokenIssuer tokenIssuer)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationFailedException(InvalidCredentialsMessage);
        }

        return await _tokenIssuer.IssueAsync(user, cancellationToken);
    }
}
