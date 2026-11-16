using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Splitwise.Api.Filters;
using Splitwise.BusinessLogic;
using Splitwise.BusinessLogic.DTOs;
using Splitwise.BusinessLogic.Exceptions;

namespace Splitwise.Tests.Api;

public class ValidationFilterTests
{
    private static ActionExecutingContext CreateContext(IDictionary<string, object?> arguments)
    {
        var services = new ServiceCollection().AddBusinessLogic().BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), arguments, controller: new object());
    }

    private static ActionExecutionDelegate Next(ActionExecutingContext context, Action onCalled)
        => () =>
        {
            onCalled();
            return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), new object()));
        };

    [Fact]
    public async Task ValidArgument_CallsNext()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["request"] = new CreateGroupRequest("Trip", null)
        });
        var nextCalled = false;

        await new ValidationFilter().OnActionExecutionAsync(context, Next(context, () => nextCalled = true));

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvalidArgument_ThrowsRequestValidationException_AndDoesNotCallNext()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["request"] = new CreateGroupRequest("", null)
        });
        var nextCalled = false;

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            new ValidationFilter().OnActionExecutionAsync(context, Next(context, () => nextCalled = true)));

        Assert.False(nextCalled);
        Assert.True(ex.Errors.ContainsKey("Name"));
    }

    [Fact]
    public async Task ErrorsFromSeveralArguments_AreCollected()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["group"] = new CreateGroupRequest("", null),
            ["member"] = new AddGroupMemberRequest("not-an-email")
        });

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            new ValidationFilter().OnActionExecutionAsync(context, Next(context, () => { })));

        Assert.True(ex.Errors.ContainsKey("Name"));
        Assert.True(ex.Errors.ContainsKey("Email"));
    }

    [Fact]
    public async Task NestedCollectionErrors_UseIndexedPropertyNames()
    {
        var request = new CreateExpenseRequest(
            1, "Dinner", 100m, null, null,
            new List<ExpenseSplitRequest> { new(1, 100m), new(0, 0m) });
        var context = CreateContext(new Dictionary<string, object?> { ["request"] = request });

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            new ValidationFilter().OnActionExecutionAsync(context, Next(context, () => { })));

        Assert.Contains("Splits[1].UserId", ex.Errors.Keys);
    }

    [Fact]
    public async Task ArgumentsWithoutValidator_AndNullArguments_AreIgnored()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["id"] = 5,                 // int: валідатора немає
            ["cancellation"] = null     // null пропускається
        });
        var nextCalled = false;

        await new ValidationFilter().OnActionExecutionAsync(context, Next(context, () => nextCalled = true));

        Assert.True(nextCalled);
    }
}
