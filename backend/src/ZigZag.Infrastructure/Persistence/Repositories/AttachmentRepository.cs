using Dapper;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Attachments.Common;
using ZigZag.Domain.Entities;
using ZigZag.Infrastructure.Persistence.Mapping;

namespace ZigZag.Infrastructure.Persistence.Repositories;

public sealed class AttachmentRepository : IAttachmentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AttachmentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string AttachmentSelectSql = """
        SELECT
            a.id,
            a.task_id AS TaskId,
            a.uploaded_by AS UploadedBy,
            u.name AS UploadedByName,
            a.file_name AS FileName,
            a.file_url AS FileUrl,
            a.file_size_bytes AS FileSizeBytes,
            a.content_type AS ContentType,
            a.created_at AS CreatedAt
        FROM task_attachments a
        JOIN users u ON u.id = a.uploaded_by
        """;

    public async Task<AttachmentDto?> GetByIdAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var sql = AttachmentSelectSql + " WHERE a.id = @AttachmentId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<AttachmentRow>(
            new CommandDefinition(sql, new { AttachmentId = attachmentId }, cancellationToken: cancellationToken));

        return row is null ? null : ToDto(row);
    }

    public async Task<IReadOnlyList<AttachmentDto>> GetForTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var sql = AttachmentSelectSql + " WHERE a.task_id = @TaskId ORDER BY a.created_at ASC;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AttachmentRow>(
            new CommandDefinition(sql, new { TaskId = taskId }, cancellationToken: cancellationToken));

        return rows.Select(ToDto).ToList();
    }

    public async Task<Guid> CreateAsync(TaskAttachment attachment, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO task_attachments (id, task_id, uploaded_by, file_name, file_url, file_size_bytes, content_type, created_at)
            VALUES (@Id, @TaskId, @UploadedBy, @FileName, @FileUrl, @FileSizeBytes, @ContentType, @CreatedAt);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, attachment, cancellationToken: cancellationToken));

        return attachment.Id;
    }

    public async Task DeleteAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM task_attachments WHERE id = @AttachmentId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { AttachmentId = attachmentId }, cancellationToken: cancellationToken));
    }

    private static AttachmentDto ToDto(AttachmentRow row) => new(
        row.Id, row.TaskId, row.UploadedBy, row.UploadedByName, row.FileName, row.FileUrl,
        row.FileSizeBytes, row.ContentType, DbDateTimeMapper.ToUtcOffset(row.CreatedAt));

    // DateTime, not DateTimeOffset, for timestamptz columns - see DbDateTimeMapper.
    private sealed record AttachmentRow(
        Guid Id, Guid TaskId, Guid UploadedBy, string UploadedByName, string FileName, string FileUrl,
        long FileSizeBytes, string ContentType, DateTime CreatedAt);
}
