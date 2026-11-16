using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Splitwise.BusinessLogic.Exceptions;

namespace Splitwise.Api.Filters;

/// <summary>
/// Автоматично валідує вхідні DTO екшенів: для кожного аргумента шукає IValidator&lt;T&gt;
/// (FluentValidation) і, якщо є помилки, кидає RequestValidationException.
/// Далі виняток перетворюється на 400 глобальним обробником помилок.
/// </summary>
/// <remarks>
/// Пакет FluentValidation.AspNetCore застарів, тому інтеграцію зроблено власним фільтром
/// (підхід "Automatic validation using a filter" з документації FluentValidation).
/// </remarks>
public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var ct = context.HttpContext.RequestAborted;
        var errors = new Dictionary<string, List<string>>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), ct);
            foreach (var failure in result.Errors)
            {
                if (!errors.TryGetValue(failure.PropertyName, out var messages))
                    errors[failure.PropertyName] = messages = new List<string>();

                if (!messages.Contains(failure.ErrorMessage))
                    messages.Add(failure.ErrorMessage);
            }
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(
                errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }

        await next();
    }
}
