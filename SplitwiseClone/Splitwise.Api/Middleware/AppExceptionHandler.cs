using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Splitwise.BusinessLogic.Exceptions;

namespace Splitwise.Api.Middleware;

/// <summary>
/// Перетворює кастомні винятки (AppException) на ProblemDetails (RFC 9457) з потрібним HTTP-статусом.
/// Помилки валідації повертаються як HttpValidationProblemDetails зі списком помилок по полях.
/// </summary>
public class AppExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;

    public AppExceptionHandler(IProblemDetailsService problemDetails)
    {
        _problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not AppException appException)
            return false;

        var (status, title) = appException switch
        {
            RequestValidationException => (StatusCodes.Status400BadRequest, "Помилка валідації"),
            NotFoundException => (StatusCodes.Status404NotFound, "Не знайдено"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Доступ заборонено"),
            ConflictException => (StatusCodes.Status409Conflict, "Конфлікт"),
            _ => (StatusCodes.Status400BadRequest, "Некоректний запит")
        };

        ProblemDetails problem = appException is RequestValidationException validation
            ? new HttpValidationProblemDetails(validation.Errors.ToDictionary(e => e.Key, e => e.Value))
            : new ProblemDetails();

        problem.Status = status;
        problem.Title = title;
        problem.Detail = appException.Message;
        problem.Extensions["code"] = appException.ErrorCode;

        httpContext.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
