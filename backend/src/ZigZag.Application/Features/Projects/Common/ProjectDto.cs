namespace ZigZag.Application.Features.Projects.Common;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    string OwnerName,
    bool IsArchived,
    int MemberCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProjectMemberDto(Guid UserId, string Name, string Email, string Role, DateTimeOffset JoinedAt);
