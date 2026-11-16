using FluentValidation.TestHelper;
using Splitwise.BusinessLogic.DTOs;
using Splitwise.BusinessLogic.Validators;

namespace Splitwise.Tests.Validators;

public class CreateGroupRequestValidatorTests
{
    private readonly CreateGroupRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(new CreateGroupRequest("Trip", "Weekend"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NullDescription_IsAllowed()
    {
        var result = _validator.TestValidate(new CreateGroupRequest("Trip", null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyName_IsInvalid(string? name)
    {
        var result = _validator.TestValidate(new CreateGroupRequest(name!, null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void TooLongName_IsInvalid()
    {
        var result = _validator.TestValidate(new CreateGroupRequest(new string('a', 101), null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameWithMaxLength_IsValid()
    {
        var result = _validator.TestValidate(new CreateGroupRequest(new string('a', 100), null));
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void TooLongDescription_IsInvalid()
    {
        var result = _validator.TestValidate(new CreateGroupRequest("Trip", new string('a', 501)));
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}

public class AddGroupMemberRequestValidatorTests
{
    private readonly AddGroupMemberRequestValidator _validator = new();

    [Fact]
    public void ValidEmail_HasNoErrors()
    {
        var result = _validator.TestValidate(new AddGroupMemberRequest("bob@example.com"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyEmail_IsInvalid(string? email)
    {
        var result = _validator.TestValidate(new AddGroupMemberRequest(email!));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("bob@")]
    [InlineData("@example.com")]
    public void MalformedEmail_IsInvalid(string email)
    {
        var result = _validator.TestValidate(new AddGroupMemberRequest(email));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
