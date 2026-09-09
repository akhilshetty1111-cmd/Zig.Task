using MediatR;
using ZigZag.Application.Features.Authentication.Common;

namespace ZigZag.Application.Features.Authentication.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RawRefreshToken) : IRequest<AuthResult>;
