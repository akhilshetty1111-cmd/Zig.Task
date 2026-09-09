namespace ZigZag.Application.Features.Tasks.Common;

/// <summary>Append-only audit trail writer/reader for a task - see docs/architecture.md.</summary>
public interface ITaskHistoryRepository
{
    Task AddAsync(
        Guid taskId, Guid changedByUserId, string fieldName, string? oldValue, string? newValue,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskHistoryEntryDto>> GetForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
}
