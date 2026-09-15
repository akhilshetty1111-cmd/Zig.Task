using FluentValidation;

namespace ZigZag.Application.Features.Attachments.Commands.UploadAttachment;

public sealed class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    // Realistic task-attachment types only - executables and scripts are
    // deliberately excluded.
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/gif", "image/webp",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain", "text/csv",
        "application/zip",
    };

    public UploadAttachmentCommandValidator()
    {
        RuleFor(c => c.TaskId).NotEmpty();
        RuleFor(c => c.FileName).NotEmpty().MaximumLength(260);
        RuleFor(c => c.FileSizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSizeBytes).WithMessage("Files must be 10 MB or smaller.");
        RuleFor(c => c.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("This file type is not supported.");
    }
}
