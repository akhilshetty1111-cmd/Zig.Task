using MediatR;
using ZigZag.Application.Features.Authentication.Common;

namespace ZigZag.Application.Features.Authentication.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<UserDto>;
