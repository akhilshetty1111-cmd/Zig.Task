using MediatR;

namespace ZigZag.Application.Features.Projects.Commands.ArchiveProject;

public sealed record ArchiveProjectCommand(Guid ProjectId) : IRequest;
