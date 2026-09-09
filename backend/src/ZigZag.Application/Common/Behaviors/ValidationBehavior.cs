using FluentValidation;
using MediatR;
using ValidationException = FluentValidation.ValidationException;

namespace ZigZag.Application.Common.Behaviors;

/// <summary>
/// Runs every registered <see cref="IValidator{T}"/> for the incoming request
/// before it reaches its handler. On failure it throws FluentValidation's own
/// <see cref="ValidationException"/> rather than a ZigZag-specific type - the
/// API's global exception handler maps that directly to 400 with the field
/// errors, so there is nothing to gain from wrapping it again.
/// </summary>
/// <remarks>
/// Registered as an open generic MediatR pipeline behavior (see
/// <c>DependencyInjection.AddApplication</c>), so every command and query gets
/// validation for free the moment a validator for it exists - a handler is
/// never reachable with invalid input, and there is no per-handler
/// boilerplate to opt in.
/// </remarks>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        // A fresh ValidationContext per validator, not one shared instance: it
        // is a stateful accumulator, not an immutable snapshot. Passing the
        // same context to multiple validators makes each one see (and re-add)
        // every prior validator's failures - confirmed empirically, since it's
        // easy to miss when a request happens to have only one validator.
        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(new ValidationContext<TRequest>(request), cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
