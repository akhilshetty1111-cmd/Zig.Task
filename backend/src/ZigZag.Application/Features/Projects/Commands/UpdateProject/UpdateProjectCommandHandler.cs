using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Projects.Commands.UpdateProject;

/// <summary>Requires the Manager role or higher - a plain Member may not rename or redescribe a project.</summary>
public sealed class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProjectAuthorizationService _authorization;

    public UpdateProjectCommandHandler(
        IProjectRepository projectRepository, ICurrentUserService currentUserService, ProjectAuthorizationService authorization)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
        _authorization = authorization;
    }

    public async Task<ProjectDto> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");

        await _authorization.EnsureRoleAsync(request.ProjectId, userId, ProjectRole.Manager, cancellationToken);

        await _projectRepository.UpdateAsync(request.ProjectId, request.Name, request.Description, cancellationToken);

        return await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);
    }
}
