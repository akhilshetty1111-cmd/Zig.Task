using FluentValidation;

namespace ZigZag.Application.Features.Authentication.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        // No complexity rules here on purpose - a login attempt should fail
        // with "invalid credentials", not leak the password policy to
        // whoever is guessing at the login form.
        RuleFor(c => c.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
