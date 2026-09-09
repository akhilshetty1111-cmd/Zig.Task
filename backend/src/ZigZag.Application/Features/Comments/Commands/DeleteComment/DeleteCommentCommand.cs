using MediatR;

namespace ZigZag.Application.Features.Comments.Commands.DeleteComment;

public sealed record DeleteCommentCommand(Guid CommentId) : IRequest;
