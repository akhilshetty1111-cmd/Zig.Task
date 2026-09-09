using MediatR;
using ZigZag.Application.Common.Exceptions;
using ZigZag.Application.Common.Interfaces;
using ZigZag.Application.Features.Projects.Common;
using ZigZag.Domain.Enums;

namespace ZigZag.Application.Features.Projects.Commands.AddProjectMember;

public sealed class AddProjectMemberCommandHandler : IRequestHandler<AddProjectMemberCommand, ProjectMemberDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ProjectAuthorizationService _authorization;

    public AddProjectMemberCommandHandler(
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        ProjectAuthorizationService authorization)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _authorization = authorization;
    }

    public async Task<ProjectMemberDto> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var callerId = _currentUserService.UserId ?? throw new AuthenticationFailedException("Not authenticated.");
        await _authorization.EnsureRoleAsync(request.ProjectId, callerId, ProjectRole.Manager, cancellationToken);

        var userToAdd = await _userRepository.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new NotFoundException($"No user found with email '{request.Email}'.");

        var existingRole = await _projectRepository.GetMemberRoleAsync(request.ProjectId, userToAdd.Id, cancellationToken);
        if (existingRole is not null)
        {
            throw new ConflictException("This user is already a member of the project.");
        }

        await _projectRepository.AddMemberAsync(request.ProjectId, userToAdd.Id, request.Role, cancellationToken);

        return new ProjectMemberDto(userToAdd.Id, userToAdd.Name, userToAdd.Email, request.Role.ToString(), DateTimeOffset.UtcNow);
    }
}
