using FluentValidation;

namespace ZigZag.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(200).WithMessage("Project name must be 200 characters or fewer.");

        RuleFor(c => c.Description)
            .MaximumLength(4000).WithMessage("Description must be 4000 characters or fewer.");
    }
}
