using Microsoft.Extensions.DependencyInjection;
using Splitwise.BusinessLogic.Interfaces;
using Splitwise.BusinessLogic.Services;

namespace Splitwise.BusinessLogic;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        return services;
    }
}
