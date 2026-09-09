namespace ZigZag.Application;

/// <summary>
/// Empty marker type used for assembly scanning at startup - MediatR handler
/// registration and FluentValidation validator discovery both need a
/// <see cref="System.Reflection.Assembly"/> reference.
/// </summary>
/// <remarks>
/// Using a dedicated marker rather than <c>typeof(SomeHandler).Assembly</c> means
/// startup wiring does not break when that arbitrary handler is renamed or moved.
/// </remarks>
public sealed class ApplicationAssemblyMarker
{
    private ApplicationAssemblyMarker()
    {
    }
}
