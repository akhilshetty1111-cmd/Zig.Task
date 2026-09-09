using FluentValidation;

namespace ZigZag.Application.Features.Authentication.Commands.RefreshToken;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(c => c.RawRefreshToken).NotEmpty().WithMessage("Refresh token is required.");
    }
}
