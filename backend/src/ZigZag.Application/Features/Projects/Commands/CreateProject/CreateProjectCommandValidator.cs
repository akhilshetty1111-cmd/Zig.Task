using FluentValidation;

namespace ZigZag.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(200).WithMessage("Project name must be 200 characters or fewer.");

        RuleFor(c => c.Description)
            .MaximumLength(4000).WithMessage("Description must be 4000 characters or fewer.");
    }
}
