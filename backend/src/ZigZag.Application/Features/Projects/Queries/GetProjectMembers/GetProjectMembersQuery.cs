using MediatR;
using ZigZag.Application.Features.Projects.Common;

namespace ZigZag.Application.Features.Projects.Queries.GetProjectMembers;

public sealed record GetProjectMembersQuery(Guid ProjectId) : IRequest<IReadOnlyList<ProjectMemberDto>>;
