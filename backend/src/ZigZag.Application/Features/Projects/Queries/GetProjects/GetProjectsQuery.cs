using MediatR;
using ZigZag.Application.Features.Projects.Common;

namespace ZigZag.Application.Features.Projects.Queries.GetProjects;

/// <summary>Every project the current user is a member of.</summary>
public sealed record GetProjectsQuery(bool IncludeArchived) : IRequest<IReadOnlyList<ProjectDto>>;
