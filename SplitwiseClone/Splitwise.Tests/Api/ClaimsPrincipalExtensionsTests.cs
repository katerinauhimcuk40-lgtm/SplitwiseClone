using System.Security.Claims;
using Splitwise.Api.Extensions;

namespace Splitwise.Tests.Api;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal PrincipalWith(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "Test"));

    [Fact]
    public void GetUserId_ReturnsIdFromNameIdentifierClaim()
    {
        var principal = PrincipalWith(new Claim(ClaimTypes.NameIdentifier, "42"));

        Assert.Equal(42, principal.GetUserId());
    }

    [Fact]
    public void GetUserId_MissingClaim_Throws()
    {
        var principal = PrincipalWith();

        Assert.Throws<InvalidOperationException>(() => principal.GetUserId());
    }

    [Fact]
    public void GetUserId_NonNumericClaim_Throws()
    {
        var principal = PrincipalWith(new Claim(ClaimTypes.NameIdentifier, "abc"));

        Assert.Throws<InvalidOperationException>(() => principal.GetUserId());
    }
}
