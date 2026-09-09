using MediatR;
using ZigZag.Application.Features.Authentication.Common;

namespace ZigZag.Application.Features.Authentication.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResult>;
