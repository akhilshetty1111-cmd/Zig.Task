using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Entities;

namespace ZigZag.Application.Features.Projects.Commands.CreateProject;

/// <summary>The creator becomes the project's Owner - see IProjectRepository.CreateAsync.</summary>
public sealed class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateProjectCommandHandler(IProjectRepository projectRepository, ICurrentUserService currentUserService)
    {
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new AuthenticationFailedException("Not authenticated.");

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            OwnerId = userId,
            IsArchived = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await _projectRepository.CreateAsync(project, cancellationToken);

        return await _projectRepository.GetByIdAsync(project.Id, cancellationToken)
            ?? throw new InvalidOperationException("Project was created but could not be read back.");
    }
}
