using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using ZigZag.API.Common;
using ZigZag.Application.Common.Exceptions;
using ValidationException = FluentValidation.ValidationException;

namespace ZigZag.UnitTests.Common;

public class ExceptionToApiResponseMapperTests
{
    [Fact]
    public void Map_ValidationException_ReturnsBadRequestWithFieldErrors()
    {
        var failures = new[]
        {
            new ValidationFailure("Email", "Email is required."),
            new ValidationFailure("Password", "Password must be at least 8 characters."),
        };
        var exception = new ValidationException(failures);

        var (statusCode, body) = ExceptionToApiResponseMapper.Map(exception, "trace-1", includeExceptionDetails: false);

        statusCode.Should().Be(StatusCodes.Status400BadRequest);
        body.Success.Should().BeFalse();
        body.Message.Should().Be("Validation failed.");
        body.Errors.Should().BeEquivalentTo(
            "Email is required.", "Password must be at least 8 characters.");
        body.TraceId.Should().Be("trace-1");
    }

    [Fact]
    public void Map_NotFoundException_ReturnsNotFoundWithMessage()
    {
        var exception = new NotFoundException("Task", Guid.Empty);

        var (statusCode, body) = ExceptionToApiResponseMapper.Map(exception, "trace-2", includeExceptionDetails: false);

        statusCode.Should().Be(StatusCodes.Status404NotFound);
        body.Message.Should().Contain("Task").And.Contain(Guid.Empty.ToString());
        body.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Map_ForbiddenAccessException_ReturnsForbidden()
    {
        var exception = new ForbiddenAccessException("Not a member of this project.");

        var (statusCode, body) = ExceptionToApiResponseMapper.Map(exception, "trace-3", includeExceptionDetails: false);

        statusCode.Should().Be(StatusCodes.Status403Forbidden);
        body.Message.Should().Be("Not a member of this project.");
    }

    [Fact]
    public void Map_ConflictException_ReturnsConflict()
    {
        var exception = new ConflictException("Email is already registered.");

        var (statusCode, body) = ExceptionToApiResponseMapper.Map(exception, "trace-4", includeExceptionDetails: false);

        statusCode.Should().Be(StatusCodes.Status409Conflict);
        body.Message.Should().Be("Email is already registered.");
    }

    [Fact]
    public void Map_UnmappedException_InProduction_ReturnsGenericMessage_NeverTheRealOne()
    {
        var exception = new InvalidOperationException("connection string contains a password: hunter2");

        var (statusCode, body) = ExceptionToApiResponseMapper.Map(exception, "trace-5", includeExceptionDetails: false);

        statusCode.Should().Be(StatusCodes.Status500InternalServerError);
        body.Message.Should().Be("An unexpected error occurred.");
        body.Message.Should().NotContain("hunter2");
    }

    [Fact]
    public void Map_UnmappedException_InDevelopment_IncludesRealMessage()
    {
        var exception = new InvalidOperationException("boom");

        var (statusCode, body) = ExceptionToApiResponseMapper.Map(exception, "trace-6", includeExceptionDetails: true);

        statusCode.Should().Be(StatusCodes.Status500InternalServerError);
        body.Message.Should().Be("boom");
    }
}
