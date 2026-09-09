using MediatR;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Tasks.Commands.ChangeTaskPriority;

public sealed record ChangeTaskPriorityCommand(Guid TaskId, TaskPriority NewPriority) : IRequest<TaskDto>;
