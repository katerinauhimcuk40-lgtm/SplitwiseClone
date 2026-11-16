using FluentValidation;
using Splitwise.BusinessLogic.DTOs;

namespace Splitwise.BusinessLogic.Validators;

public class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(x => x.GroupId)
            .GreaterThan(0).WithMessage("Некоректний id групи.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Опис витрати обов'язковий.")
            .MaximumLength(200).WithMessage("Опис витрати не може бути довшим за 200 символів.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Сума витрати має бути більшою за нуль.")
            .Must(HasAtMostTwoDecimals).WithMessage("Сума може мати не більше двох знаків після коми.");

        RuleFor(x => x.PaidById)
            .GreaterThan(0).When(x => x.PaidById.HasValue)
            .WithMessage("Некоректний id платника.");

        // Якщо частки вказано — перевіряємо кожну та узгодженість із загальною сумою.
        When(x => x.Splits is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.Splits!).SetValidator(new ExpenseSplitRequestValidator());

            RuleFor(x => x.Splits!)
                .Must(splits => splits.Select(s => s.UserId).Distinct().Count() == splits.Count)
                .WithMessage("Користувач не може бути вказаний у розподілі більше одного разу.");

            RuleFor(x => x.Splits!)
                .Must((request, splits) => splits.Sum(s => s.Amount) == request.Amount)
                .WithMessage("Сума часток має дорівнювати сумі витрати.");
        });
    }

    internal static bool HasAtMostTwoDecimals(decimal value) => decimal.Round(value, 2) == value;
}

public class ExpenseSplitRequestValidator : AbstractValidator<ExpenseSplitRequest>
{
    public ExpenseSplitRequestValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("Некоректний id користувача у розподілі.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Частка має бути більшою за нуль.")
            .Must(CreateExpenseRequestValidator.HasAtMostTwoDecimals)
            .WithMessage("Частка може мати не більше двох знаків після коми.");
    }
}
