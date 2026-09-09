using FluentValidation;

namespace ZigZag.Application.Features.Comments.Commands.AddComment;

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(c => c.TaskId).NotEmpty();
        RuleFor(c => c.Text)
            .NotEmpty().WithMessage("Comment text is required.")
            .MaximumLength(4000).WithMessage("Comment must be 4000 characters or fewer.");
    }
}
