using System.Security.Claims;

namespace Splitwise.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id поточного користувача з claim NameIdentifier (його додає ASP.NET Core Identity).</summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("У токені відсутній валідний ідентифікатор користувача.");
    }
}
