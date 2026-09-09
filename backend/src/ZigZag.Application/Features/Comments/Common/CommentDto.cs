namespace ZigZag.Application.Features.Comments.Common;

public sealed record CommentDto(
    Guid Id, Guid TaskId, Guid UserId, string UserName, string Text, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
