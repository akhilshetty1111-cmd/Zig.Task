using MediatR;
using ZigZag.Application.Features.Authentication.Common;

namespace ZigZag.Application.Features.Authentication.Commands.Register;

/// <summary>
/// Creates a new account and immediately issues tokens (auto-login) rather
/// than requiring a separate login call right after - the smoother UX for a
/// SPA, and there's no security reason to force a second round trip for
/// credentials the caller just supplied.
/// </summary>
public sealed record RegisterCommand(string Name, string Email, string Password) : IRequest<AuthResult>;
