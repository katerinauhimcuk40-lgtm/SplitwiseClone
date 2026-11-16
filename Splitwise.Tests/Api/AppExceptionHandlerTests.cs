using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Splitwise.Api.Middleware;
using Splitwise.BusinessLogic.Exceptions;

namespace Splitwise.Tests.Api;

/// <summary>Мінімальна заглушка IProblemDetailsService, що запам'ятовує, що саме "записали".</summary>
internal sealed class FakeProblemDetailsService : IProblemDetailsService
{
    public ProblemDetailsContext? Context { get; private set; }

    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        Context = context;
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
    {
        Context = context;
        return ValueTask.FromResult(true);
    }
}

public class AppExceptionHandlerTests
{
    public static IEnumerable<object[]> StatusMappings => new[]
    {
        new object[] { new GroupNotFoundException(1), 404, "GroupNotFound" },
        new object[] { new ExpenseNotFoundException(1), 404, "ExpenseNotFound" },
        new object[] { new UserNotFoundException(), 404, "UserNotFound" },
        new object[] { new NotGroupMemberException(), 403, "NotGroupMember" },
        new object[] { new ExpenseDeletionNotAllowedException(), 403, "ExpenseDeletionNotAllowed" },
        new object[] { new UserAlreadyInGroupException(), 409, "UserAlreadyInGroup" },
        new object[] { new InvalidExpenseException("bad"), 400, "InvalidExpense" },
        new object[] { new BusinessRuleException("bad"), 400, "BusinessRule" },
    };

    [Theory]
    [MemberData(nameof(StatusMappings))]
    public async Task TryHandleAsync_MapsAppExceptionToStatusAndCode(AppException exception, int expectedStatus, string expectedCode)
    {
        var writer = new FakeProblemDetailsService();
        var handler = new AppExceptionHandler(writer);
        var httpContext = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatus, httpContext.Response.StatusCode);
        Assert.NotNull(writer.Context);
        Assert.Equal(expectedStatus, writer.Context!.ProblemDetails.Status);
        Assert.Equal(exception.Message, writer.Context.ProblemDetails.Detail);
        Assert.Equal(expectedCode, writer.Context.ProblemDetails.Extensions["code"]);
    }

    [Fact]
    public async Task TryHandleAsync_ValidationException_ReturnsErrorsPerField()
    {
        var writer = new FakeProblemDetailsService();
        var handler = new AppExceptionHandler(writer);
        var httpContext = new DefaultHttpContext();
        var exception = new RequestValidationException(new Dictionary<string, string[]>
        {
            ["Name"] = new[] { "Name is required." },
            ["Amount"] = new[] { "Must be positive.", "Too many decimals." }
        });

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        var problem = Assert.IsType<HttpValidationProblemDetails>(writer.Context!.ProblemDetails);
        Assert.Equal(new[] { "Name is required." }, problem.Errors["Name"]);
        Assert.Equal(2, problem.Errors["Amount"].Length);
        Assert.Equal("RequestValidation", problem.Extensions["code"]);
    }

    [Fact]
    public async Task TryHandleAsync_UnknownException_ReturnsFalse_AndWritesNothing()
    {
        var writer = new FakeProblemDetailsService();
        var handler = new AppExceptionHandler(writer);
        var httpContext = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(httpContext, new InvalidOperationException("boom"), CancellationToken.None);

        Assert.False(handled);
        Assert.Null(writer.Context);
    }
}

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_Returns500_WithoutLeakingExceptionDetails()
    {
        var writer = new FakeProblemDetailsService();
        var handler = new GlobalExceptionHandler(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GlobalExceptionHandler>.Instance, writer);
        var httpContext = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(
            httpContext, new InvalidOperationException("secret connection string"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        var problem = writer.Context!.ProblemDetails;
        Assert.Equal(500, problem.Status);
        Assert.DoesNotContain("secret", problem.Detail);
    }
}

public class AppExceptionTests
{
    [Fact]
    public void ErrorCode_IsTypeNameWithoutExceptionSuffix()
    {
        Assert.Equal("GroupNotFound", new GroupNotFoundException(5).ErrorCode);
        Assert.Equal("NotGroupMember", new NotGroupMemberException().ErrorCode);
    }

    [Fact]
    public void SpecificExceptions_DeriveFromCategoryExceptions()
    {
        Assert.IsAssignableFrom<NotFoundException>(new GroupNotFoundException(1));
        Assert.IsAssignableFrom<ForbiddenException>(new NotGroupMemberException());
        Assert.IsAssignableFrom<ConflictException>(new UserAlreadyInGroupException());
        Assert.IsAssignableFrom<BusinessRuleException>(new InvalidExpenseException("x"));
        Assert.IsAssignableFrom<AppException>(new RequestValidationException(new Dictionary<string, string[]>()));
    }

    [Fact]
    public void GroupNotFoundException_MessageContainsId()
    {
        Assert.Contains("42", new GroupNotFoundException(42).Message);
    }

    [Fact]
    public void RequestValidationException_ExposesErrors()
    {
        var ex = new RequestValidationException(new Dictionary<string, string[]> { ["Name"] = new[] { "required" } });

        Assert.Equal("required", ex.Errors["Name"].Single());
    }
}
