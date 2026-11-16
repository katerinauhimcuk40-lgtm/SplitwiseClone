using FluentValidation.TestHelper;
using Splitwise.BusinessLogic.DTOs;
using Splitwise.BusinessLogic.Validators;

namespace Splitwise.Tests.Validators;

public class CreateExpenseRequestValidatorTests
{
    private readonly CreateExpenseRequestValidator _validator = new();

    private static CreateExpenseRequest Request(
        int groupId = 1,
        string description = "Dinner",
        decimal amount = 100m,
        int? paidById = null,
        List<ExpenseSplitRequest>? splits = null)
        => new(groupId, description, amount, null, paidById, splits);

    [Fact]
    public void ValidRequest_WithoutSplits_HasNoErrors()
    {
        _validator.TestValidate(Request()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidRequest_WithMatchingSplits_HasNoErrors()
    {
        var splits = new List<ExpenseSplitRequest> { new(1, 60m), new(2, 40m) };
        _validator.TestValidate(Request(splits: splits)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveGroupId_IsInvalid(int groupId)
    {
        _validator.TestValidate(Request(groupId: groupId)).ShouldHaveValidationErrorFor(x => x.GroupId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void EmptyDescription_IsInvalid(string? description)
    {
        _validator.TestValidate(Request(description: description!)).ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void TooLongDescription_IsInvalid()
    {
        _validator.TestValidate(Request(description: new string('a', 201)))
            .ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NonPositiveAmount_IsInvalid(double amount)
    {
        _validator.TestValidate(Request(amount: (decimal)amount)).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void AmountWithMoreThanTwoDecimals_IsInvalid()
    {
        _validator.TestValidate(Request(amount: 10.005m)).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void AmountWithTwoDecimals_IsValid()
    {
        _validator.TestValidate(Request(amount: 10.05m)).ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void NonPositivePayerId_IsInvalid()
    {
        _validator.TestValidate(Request(paidById: 0)).ShouldHaveValidationErrorFor(x => x.PaidById);
    }

    [Fact]
    public void SplitsNotMatchingAmount_IsInvalid()
    {
        var splits = new List<ExpenseSplitRequest> { new(1, 50m), new(2, 40m) };
        _validator.TestValidate(Request(amount: 100m, splits: splits)).ShouldHaveValidationErrorFor("Splits");
    }

    [Fact]
    public void DuplicateUsersInSplits_IsInvalid()
    {
        var splits = new List<ExpenseSplitRequest> { new(1, 50m), new(1, 50m) };
        _validator.TestValidate(Request(amount: 100m, splits: splits)).ShouldHaveValidationErrorFor("Splits");
    }

    [Fact]
    public void InvalidSplitItem_ReportsIndexedProperty()
    {
        var splits = new List<ExpenseSplitRequest> { new(1, 100m), new(0, 0m) };
        var result = _validator.TestValidate(Request(amount: 100m, splits: splits));

        result.ShouldHaveValidationErrorFor("Splits[1].UserId");
        result.ShouldHaveValidationErrorFor("Splits[1].Amount");
    }

    [Fact]
    public void EmptySplitsList_IsTreatedAsEqualSplit_AndIsValid()
    {
        _validator.TestValidate(Request(splits: new List<ExpenseSplitRequest>()))
            .ShouldNotHaveAnyValidationErrors();
    }
}

public class ExpenseSplitRequestValidatorTests
{
    private readonly ExpenseSplitRequestValidator _validator = new();

    [Fact]
    public void ValidSplit_HasNoErrors()
    {
        _validator.TestValidate(new ExpenseSplitRequest(1, 10.50m)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NonPositiveUserId_IsInvalid()
    {
        _validator.TestValidate(new ExpenseSplitRequest(0, 10m)).ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void NonPositiveAmount_IsInvalid()
    {
        _validator.TestValidate(new ExpenseSplitRequest(1, 0m)).ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void AmountWithMoreThanTwoDecimals_IsInvalid()
    {
        _validator.TestValidate(new ExpenseSplitRequest(1, 1.234m)).ShouldHaveValidationErrorFor(x => x.Amount);
    }
}
