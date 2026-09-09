using MediatR;
using ZigZag.Application.Features.Projects.Common;

namespace ZigZag.Application.Features.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(string Name, string? Description) : IRequest<ProjectDto>;
