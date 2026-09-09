using MediatR;
using ZigZag.Application.Features.Tasks.Common;

namespace ZigZag.Application.Features.Tasks.Queries.GetTaskById;

public sealed record GetTaskByIdQuery(Guid TaskId) : IRequest<TaskDto>;
