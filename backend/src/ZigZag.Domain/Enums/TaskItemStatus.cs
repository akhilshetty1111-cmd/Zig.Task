namespace ZigZag.Domain.Enums;

/// <summary>
/// Lifecycle state of a task. Persisted in PostgreSQL as the SCREAMING_SNAKE_CASE
/// strings TODO / IN_PROGRESS / IN_REVIEW / DONE / BLOCKED, which are enforced by a
/// CHECK constraint on the tasks table. The enum-to-string translation lives in a
/// Dapper type handler in the Infrastructure layer, so Domain stays persistence-free.
/// </summary>
/// <remarks>
/// Named <c>TaskItemStatus</c> rather than <c>TaskStatus</c> to avoid colliding with
/// <see cref="System.Threading.Tasks.TaskStatus"/>, which implicit usings pull into scope.
/// </remarks>
public enum TaskItemStatus
{
    Todo = 0,
    InProgress = 1,
    InReview = 2,
    Done = 3,
    Blocked = 4
}
