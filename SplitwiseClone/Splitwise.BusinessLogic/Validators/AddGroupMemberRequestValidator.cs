using FluentValidation;
using Splitwise.BusinessLogic.DTOs;

namespace Splitwise.BusinessLogic.Validators;

public class AddGroupMemberRequestValidator : AbstractValidator<AddGroupMemberRequest>
{
    public AddGroupMemberRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обов'язковий.")
            .EmailAddress().WithMessage("Некоректний формат email.");
    }
}
