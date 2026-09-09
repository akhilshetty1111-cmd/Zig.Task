using FluentValidation.TestHelper;
using ZigZag.Application.Features.Authentication.Commands.Register;

namespace ZigZag.UnitTests.Features.Authentication;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
        => _validator.TestValidate(new RegisterCommand("Alice", "alice@zigzag.dev", "Passw0rd!"))
            .ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_InvalidEmail_HasError(string email)
        => _validator.TestValidate(new RegisterCommand("Alice", email, "Passw0rd!"))
            .ShouldHaveValidationErrorFor(c => c.Email);

    [Fact]
    public void Validate_BlankName_HasError()
        => _validator.TestValidate(new RegisterCommand("", "alice@zigzag.dev", "Passw0rd!"))
            .ShouldHaveValidationErrorFor(c => c.Name);

    [Theory]
    [InlineData("short1A")]      // too short
    [InlineData("alllowercase1")] // no uppercase
    [InlineData("ALLUPPERCASE1")] // no lowercase
    [InlineData("NoDigitsHere")]  // no digit
    public void Validate_WeakPassword_HasError(string password)
        => _validator.TestValidate(new RegisterCommand("Alice", "alice@zigzag.dev", password))
            .ShouldHaveValidationErrorFor(c => c.Password);
}
