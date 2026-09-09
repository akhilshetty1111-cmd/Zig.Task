namespace ZigZag.Application.Features.Authentication.Common;

public sealed record UserDto(Guid Id, string Name, string Email, string Role);
