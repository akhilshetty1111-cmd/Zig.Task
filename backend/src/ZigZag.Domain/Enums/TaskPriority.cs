namespace ZigZag.Domain.Enums;

/// <summary>
/// Task priority. Persisted as LOW / MEDIUM / HIGH / URGENT.
/// Ordinal values ascend with urgency so sorting by priority is a plain integer sort.
/// </summary>
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}
