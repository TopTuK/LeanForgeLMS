using System.Security.Claims;
using LF.WebApi.Common;

namespace LF.WebApiTests.Common;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal PrincipalWith(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Test"));

    [Fact]
    public void GetUserId_ValidNumericNameIdentifier_ReturnsId()
    {
        var user = PrincipalWith(new Claim(ClaimTypes.NameIdentifier, "42"));

        Assert.Equal(42, user.GetUserId());
    }

    [Fact]
    public void GetUserId_NoNameIdentifierClaim_ReturnsNull()
    {
        var user = PrincipalWith(new Claim("email", "a@b.com"));

        Assert.Null(user.GetUserId());
    }

    [Fact]
    public void GetUserId_NonNumericNameIdentifier_ReturnsNull()
    {
        var user = PrincipalWith(new Claim(ClaimTypes.NameIdentifier, "not-a-number"));

        Assert.Null(user.GetUserId());
    }

    [Fact]
    public void GetUserId_EmptyPrincipal_ReturnsNull()
    {
        Assert.Null(new ClaimsPrincipal().GetUserId());
    }
}
