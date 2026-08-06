using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LF.Application.ModelDto.Authentication;
using LF.Application.Services.Authentication;

namespace LF.ApplicationTests.Services.Authentication;

public class TokenServiceTests
{
    private static JwtTokenConfigDto CreateConfig(int expiresDays = 7) => new()
    {
        Issuer = "lf-issuer",
        Audience = "lf-audience",
        Key = "a-sufficiently-long-signing-key-for-hmac-sha256!",
        ExpiresDays = expiresDays,
    };

    [Fact]
    public void CreateWebJwtToken_ValidConfig_ProducesTokenWithExpectedIssuerAudienceAndClaims()
    {
        // Arrange
        var service = new TokenService();
        var config = CreateConfig();
        Claim[] claims = [new(ClaimTypes.NameIdentifier, "42"), new(ClaimTypes.Email, "a@b.com")];

        // Act
        var result = service.CreateWebJwtToken(claims, config);

        // Assert
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal(config.Issuer, token.Issuer);
        Assert.Contains(config.Audience, token.Audiences);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "42");
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Email && c.Value == "a@b.com");
    }

    [Fact]
    public void CreateWebJwtToken_SetsExpiryFromConfig()
    {
        // Arrange
        var service = new TokenService();
        var config = CreateConfig(expiresDays: 1);

        // Act
        var before = DateTime.UtcNow.AddDays(config.ExpiresDays);
        var result = service.CreateWebJwtToken([], config);
        var after = DateTime.UtcNow.AddDays(config.ExpiresDays);

        // Assert
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.InRange(token.ValidTo, before.AddSeconds(-5), after.AddSeconds(5));
    }
}
