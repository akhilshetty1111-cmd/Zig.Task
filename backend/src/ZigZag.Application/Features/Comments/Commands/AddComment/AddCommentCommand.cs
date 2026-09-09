using MediatR;
using ZigZag.Application.Features.Comments.Common;

namespace ZigZag.Application.Features.Comments.Commands.AddComment;

public sealed record AddCommentCommand(Guid TaskId, string Text) : IRequest<CommentDto>;
