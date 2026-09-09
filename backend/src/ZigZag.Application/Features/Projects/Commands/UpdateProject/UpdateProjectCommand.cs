using MediatR;
using ZigZag.Application.Features.Projects.Common;

namespace ZigZag.Application.Features.Projects.Commands.UpdateProject;

public sealed record UpdateProjectCommand(Guid ProjectId, string Name, string? Description) : IRequest<ProjectDto>;
