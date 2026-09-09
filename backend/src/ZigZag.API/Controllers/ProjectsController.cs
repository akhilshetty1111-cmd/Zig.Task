using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZigZag.Application.Features.Projects.Commands.AddProjectMember;
using ZigZag.Application.Features.Projects.Commands.ArchiveProject;
using ZigZag.Application.Features.Projects.Commands.CreateProject;
using ZigZag.Application.Features.Projects.Commands.RemoveProjectMember;
using ZigZag.Application.Features.Projects.Commands.UpdateProject;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Application.Features.Projects.Queries.GetProjectById;
using ZigZag.Application.Features.Projects.Queries.GetProjectMembers;
using ZigZag.Application.Features.Projects.Queries.GetProjects;
using ZigZag.Domain.Enums;

namespace ZigZag.API.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public sealed class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Projects the current user is a member of.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> GetProjects(
        [FromQuery] bool includeArchived, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetProjectsQuery(includeArchived), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetProjectByIdQuery(id), cancellationToken));

    /// <summary>The caller becomes the project's Owner.</summary>
    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await _mediator.Send(new CreateProjectCommand(request.Name, request.Description), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> Update(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new UpdateProjectCommand(id, request.Name, request.Description), cancellationToken));

    /// <summary>Archives the project (soft-delete) - see docs/architecture.md for why this isn't a hard delete.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ArchiveProjectCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<ProjectMemberDto>>> GetMembers(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetProjectMembersQuery(id), cancellationToken));

    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<ProjectMemberDto>> AddMember(
        Guid id, AddProjectMemberRequest request, CancellationToken cancellationToken)
    {
        var member = await _mediator.Send(new AddProjectMemberCommand(id, request.Email, request.Role), cancellationToken);
        return CreatedAtAction(nameof(GetMembers), new { id }, member);
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveProjectMemberCommand(id, userId), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateProjectRequest(string Name, string? Description);

public sealed record UpdateProjectRequest(string Name, string? Description);

public sealed record AddProjectMemberRequest(string Email, ProjectRole Role);
