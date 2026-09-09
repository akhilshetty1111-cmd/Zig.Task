using MediatR;
using ZigZag.Application.Features.Projects.Common;

namespace ZigZag.Application.Features.Projects.Queries.GetProjectById;

public sealed record GetProjectByIdQuery(Guid ProjectId) : IRequest<ProjectDto>;
