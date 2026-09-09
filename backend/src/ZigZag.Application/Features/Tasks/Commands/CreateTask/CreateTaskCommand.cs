using MediatR;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Tasks.Commands.CreateTask;

public sealed record CreateTaskCommand(
    Guid ProjectId,
    string Title,
    string? Description,
    TaskPriority Priority,
    Guid? AssignedToUserId,
    DateOnly? DueDate) : IRequest<TaskDto>;
