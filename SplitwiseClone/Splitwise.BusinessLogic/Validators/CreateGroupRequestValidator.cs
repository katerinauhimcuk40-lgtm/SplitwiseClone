using FluentValidation;
using Splitwise.BusinessLogic.DTOs;

namespace Splitwise.BusinessLogic.Validators;

public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва групи обов'язкова.")
            .MaximumLength(100).WithMessage("Назва групи не може бути довшою за 100 символів.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Опис групи не може бути довшим за 500 символів.");
    }
}
