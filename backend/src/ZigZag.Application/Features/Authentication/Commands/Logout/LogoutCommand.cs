using MediatR;

namespace ZigZag.Application.Features.Authentication.Commands.Logout;

public sealed record LogoutCommand(string RawRefreshToken) : IRequest;
