using MediatR;
using ZigZag.Application.Features.Comments.Common;

namespace ZigZag.Application.Features.Comments.Queries.GetComments;

public sealed record GetCommentsQuery(Guid TaskId) : IRequest<IReadOnlyList<CommentDto>>;
