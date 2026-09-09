using MediatR;

namespace ZigZag.Application.Features.Projects.Commands.RemoveProjectMember;

public sealed record RemoveProjectMemberCommand(Guid ProjectId, Guid UserId) : IRequest;
