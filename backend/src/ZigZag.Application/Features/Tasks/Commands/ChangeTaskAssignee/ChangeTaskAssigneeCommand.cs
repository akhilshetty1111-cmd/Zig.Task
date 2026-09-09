using MediatR;
using ZigZag.Application.Features.Tasks.Common;

namespace ZigZag.Application.Features.Tasks.Commands.ChangeTaskAssignee;

public sealed record ChangeTaskAssigneeCommand(Guid TaskId, Guid? AssignedToUserId) : IRequest<TaskDto>;
