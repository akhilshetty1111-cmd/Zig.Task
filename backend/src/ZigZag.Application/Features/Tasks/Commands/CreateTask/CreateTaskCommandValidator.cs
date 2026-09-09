using FluentValidation;

namespace ZigZag.Application.Features.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();

        RuleFor(c => c.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(300).WithMessage("Task title must be 300 characters or fewer.");

        RuleFor(c => c.Priority).IsInEnum();
    }
}
