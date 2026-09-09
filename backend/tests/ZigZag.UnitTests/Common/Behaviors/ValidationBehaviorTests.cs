using FluentValidation;
using MediatR;
using ZigZag.Application.Common.Behaviors;
using ValidationException = FluentValidation.ValidationException;

namespace ZigZag.UnitTests.Common.Behaviors;

/// <summary>
/// A request/handler/validator triple that exists only for this test file -
/// Application ships no demo feature, since ValidationBehavior is generic
/// over any IRequest and needs nothing feature-specific to exercise it.
/// </summary>
internal sealed record SampleCommand(string Name) : IRequest<string>;

internal sealed class SampleCommandValidator : AbstractValidator<SampleCommand>
{
    public SampleCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().WithMessage("Name is required.");
    }
}

internal sealed class SampleCommandMinLengthValidator : AbstractValidator<SampleCommand>
{
    public SampleCommandMinLengthValidator()
    {
        RuleFor(c => c.Name).MinimumLength(3).WithMessage("Name must be at least 3 characters.");
    }
}

public class ValidationBehaviorTests
{
    private static Task<string> CallHandler(SampleCommand request, CancellationToken _)
        => Task.FromResult($"handled: {request.Name}");

    [Fact]
    public async Task Handle_NoValidatorsRegistered_CallsNext()
    {
        var behavior = new ValidationBehavior<SampleCommand, string>(validators: []);
        var request = new SampleCommand("irrelevant");

        var result = await behavior.Handle(request, () => CallHandler(request, CancellationToken.None), CancellationToken.None);

        result.Should().Be("handled: irrelevant");
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<SampleCommand, string>([new SampleCommandValidator()]);
        var request = new SampleCommand("Alice");

        var result = await behavior.Handle(request, () => CallHandler(request, CancellationToken.None), CancellationToken.None);

        result.Should().Be("handled: Alice");
    }

    [Fact]
    public async Task Handle_InvalidRequest_ThrowsValidationException_AndNeverCallsNext()
    {
        var behavior = new ValidationBehavior<SampleCommand, string>([new SampleCommandValidator()]);
        var request = new SampleCommand("");
        var nextWasCalled = false;

        Func<Task> act = () => behavior.Handle(request, () =>
        {
            nextWasCalled = true;
            return CallHandler(request, CancellationToken.None);
        }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainSingle(e => e.ErrorMessage == "Name is required.");
        nextWasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_MultipleValidators_AggregatesFailuresFromAll()
    {
        var behavior = new ValidationBehavior<SampleCommand, string>(
        [
            new SampleCommandValidator(),
            new SampleCommandMinLengthValidator(),
        ]);
        var request = new SampleCommand("");

        Func<Task> act = () => behavior.Handle(request, () => CallHandler(request, CancellationToken.None), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().HaveCount(2);
    }
}
