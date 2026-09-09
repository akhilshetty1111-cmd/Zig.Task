using FluentValidation;

namespace ZigZag.Application.Features.Projects.Commands.AddProjectMember;

public sealed class AddProjectMemberCommandValidator : AbstractValidator<AddProjectMemberCommand>
{
    public AddProjectMemberCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
        RuleFor(c => c.Role).IsInEnum();
    }
}
