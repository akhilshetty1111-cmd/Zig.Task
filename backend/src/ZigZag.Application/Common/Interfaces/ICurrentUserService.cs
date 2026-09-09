namespace ZigZag.Application.Common.Interfaces;

/// <summary>
/// Reads the authenticated caller's identity from the current request.
/// Implemented in ZigZag.API (it needs IHttpContextAccessor, an ASP.NET Core
/// concept Application must not depend on) and consumed by query/command
/// handlers that need "who is calling" - starting with GetCurrentUserQuery
/// here, and by every project/task authorization check from Phase 5 onward.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }
}
