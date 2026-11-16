using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Splitwise.BusinessLogic.Interfaces;
using Splitwise.BusinessLogic.Services;
using Splitwise.BusinessLogic.Validators;

namespace Splitwise.BusinessLogic;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IExpenseService, ExpenseService>();

        // Реєструє всі AbstractValidator<T> зі збірки BusinessLogic.
        services.AddValidatorsFromAssemblyContaining<CreateGroupRequestValidator>();
        return services;
    }
}
