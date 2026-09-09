namespace ZigZag.API.Common;

/// <summary>
/// The JSON shape every ZigZag endpoint returns on failure. Success responses
/// are not wrapped - a handler's DTO is returned directly with the
/// appropriate 2xx status - so this type exists purely for the API layer's
/// global exception handler; see docs/api.md and docs/architecture.md,
/// decision 11.
/// </summary>
/// <param name="Message">Human-readable summary, safe to show a user.</param>
/// <param name="Errors">Field-level validation messages; empty for non-validation failures.</param>
/// <param name="TraceId">Correlates this response with the Serilog entry and Application Insights trace.</param>
public sealed record ApiErrorResponse(string Message, IReadOnlyList<string> Errors, string TraceId)
{
    public bool Success { get; }
}
