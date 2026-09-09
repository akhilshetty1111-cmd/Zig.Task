using Dapper;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Notifications.Common;
using ZigZag.Domain.Entities;
using ZigZag.Infrastructure.Persistence.Mapping;

namespace ZigZag.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public NotificationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> CreateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO notifications (id, user_id, type, title, message, task_id, project_id, is_read, created_at)
            VALUES (@Id, @UserId, @Type, @Title, @Message, @TaskId, @ProjectId, @IsRead, @CreatedAt);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, notification, cancellationToken: cancellationToken));

        return notification.Id;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetForUserAsync(
        Guid userId, bool unreadOnly, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id, type, title, message,
                task_id AS TaskId, project_id AS ProjectId, is_read AS IsRead, created_at AS CreatedAt
            FROM notifications
            WHERE user_id = @UserId AND (@UnreadOnly = false OR is_read = false)
            ORDER BY created_at DESC;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<NotificationRow>(
            new CommandDefinition(sql, new { UserId = userId, UnreadOnly = unreadOnly }, cancellationToken: cancellationToken));

        return rows.Select(r => new NotificationDto(
            r.Id, r.Type, r.Title, r.Message, r.TaskId, r.ProjectId, r.IsRead, DbDateTimeMapper.ToUtcOffset(r.CreatedAt)))
            .ToList();
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE notifications SET is_read = true WHERE id = @NotificationId AND user_id = @UserId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { NotificationId = notificationId, UserId = userId }, cancellationToken: cancellationToken));
    }

    // DateTime, not DateTimeOffset, for the created_at timestamptz column -
    // see DbDateTimeMapper's remarks for why.
    private sealed record NotificationRow(
        Guid Id, string Type, string Title, string? Message, Guid? TaskId, Guid? ProjectId, bool IsRead, DateTime CreatedAt);
}
