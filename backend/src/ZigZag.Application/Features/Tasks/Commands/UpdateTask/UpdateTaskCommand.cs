using MediatR;
using ZigZag.Application.Features.Tasks.Common;

namespace ZigZag.Application.Features.Tasks.Commands.UpdateTask;

public sealed record UpdateTaskCommand(Guid TaskId, string Title, string? Description, DateOnly? DueDate) : IRequest<TaskDto>;
