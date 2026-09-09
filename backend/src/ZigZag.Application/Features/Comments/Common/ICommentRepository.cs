using ZigZag.Domain.Entities;

namespace ZigZag.Application.Features.Comments.Common;

public interface ICommentRepository
{
    Task<CommentDto?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommentDto>> GetForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(Comment comment, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid commentId, CancellationToken cancellationToken = default);
}
