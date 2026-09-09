using ZigZag.Application.Common.Models;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Domain.Entities;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Common.Interfaces;

/// <summary>
/// Task persistence. Lives in Common because Comments (Phase 8) and the
/// Dashboard (Phase 9) both need to read tasks too.
/// </summary>
public interface ITaskRepository
{
    Task<TaskDto?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<PagedResult<TaskDto>> GetForProjectAsync(TaskQueryFilter filter, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(TaskItem task, CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid taskId, string title, string? description, DateOnly? dueDate, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Also sets/clears completed_at to match the new status in the same
    /// statement - the tasks_completed_at_matches_status CHECK constraint
    /// requires them to change together atomically.
    /// </summary>
    Task ChangeStatusAsync(Guid taskId, TaskItemStatus newStatus, CancellationToken cancellationToken = default);

    Task ChangeAssigneeAsync(Guid taskId, Guid? assignedToUserId, CancellationToken cancellationToken = default);

    Task ChangePriorityAsync(Guid taskId, TaskPriority newPriority, CancellationToken cancellationToken = default);
}
