using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZigZag.Application.Common.Models;
using ZigZag.Application.Features.Tasks.Commands.ChangeTaskAssignee;
using ZigZag.Application.Features.Tasks.Commands.ChangeTaskPriority;
using ZigZag.Application.Features.Tasks.Commands.ChangeTaskStatus;
using ZigZag.Application.Features.Tasks.Commands.CreateTask;
using ZigZag.Application.Features.Tasks.Commands.DeleteTask;
using ZigZag.Application.Features.Tasks.Commands.UpdateTask;
using ZigZag.Application.Features.Tasks.Common;
using ZigZag.Application.Features.Tasks.Queries.GetTaskById;
using ZigZag.Application.Features.Tasks.Queries.GetTasks;
using ZigZag.Domain.Enums;

namespace ZigZag.API.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public sealed class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Server-side filtering, searching, sorting and pagination, all scoped to
    /// one project - e.g. GET /api/tasks?projectId=...&amp;status=IN_PROGRESS&amp;pageNumber=1&amp;pageSize=20.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskDto>>> GetTasks(
        [FromQuery] Guid projectId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TaskItemStatus? status = null,
        [FromQuery] TaskPriority? priority = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? assignedTo = null,
        [FromQuery] TaskSortBy sortBy = TaskSortBy.CreatedAt,
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTasksQuery(
            projectId, pageNumber, pageSize, status, priority, search, assignedTo, sortBy, sortDescending);
        return Ok(await _mediator.Send(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetTaskByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTaskCommand(
            request.ProjectId, request.Title, request.Description,
            request.Priority ?? TaskPriority.Medium, request.AssignedToUserId, request.DueDate);
        var task = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskDto>> Update(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new UpdateTaskCommand(id, request.Title, request.Description, request.DueDate), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteTaskCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>The Kanban drag-and-drop endpoint.</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<TaskDto>> ChangeStatus(Guid id, ChangeStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ChangeTaskStatusCommand(id, request.Status), cancellationToken));

    [HttpPatch("{id:guid}/assignee")]
    public async Task<ActionResult<TaskDto>> ChangeAssignee(Guid id, ChangeAssigneeRequest request, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ChangeTaskAssigneeCommand(id, request.AssignedToUserId), cancellationToken));

    [HttpPatch("{id:guid}/priority")]
    public async Task<ActionResult<TaskDto>> ChangePriority(Guid id, ChangePriorityRequest request, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ChangeTaskPriorityCommand(id, request.Priority), cancellationToken));
}

public sealed record CreateTaskRequest(
    Guid ProjectId, string Title, string? Description, TaskPriority? Priority, Guid? AssignedToUserId, DateOnly? DueDate);

public sealed record UpdateTaskRequest(string Title, string? Description, DateOnly? DueDate);

public sealed record ChangeStatusRequest(TaskItemStatus Status);

public sealed record ChangeAssigneeRequest(Guid? AssignedToUserId);

public sealed record ChangePriorityRequest(TaskPriority Priority);
