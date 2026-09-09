using MediatR;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Projects.Commands.AddProjectMember;

public sealed record AddProjectMemberCommand(Guid ProjectId, string Email, ProjectRole Role) : IRequest<ProjectMemberDto>;
