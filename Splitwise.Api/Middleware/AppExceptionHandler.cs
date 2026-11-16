using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Splitwise.BusinessLogic.Exceptions;

namespace Splitwise.Api.Middleware;

/// <summary>Перетворює винятки бізнес-логіки на відповіді ProblemDetails з відповідним HTTP-статусом.</summary>
public class AppExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not AppException)
            return false;

        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Не знайдено"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Доступ заборонено"),
            ConflictException => (StatusCodes.Status409Conflict, "Конфлікт"),
            _ => (StatusCodes.Status400BadRequest, "Некоректний запит")
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = title, Detail = exception.Message },
            cancellationToken);

        return true;
    }
}
