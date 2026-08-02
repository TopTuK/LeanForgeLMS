using LF.Application.ModelDto.Authentication;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace LF.Application.Services.Authentication;

internal sealed class TokenService : ITokenService
{
    private static SymmetricSecurityKey GetSymmetricSecurityKey(string key) => new(Encoding.UTF8.GetBytes(key));

    public JwtTokenDto CreateWebJwtToken(IEnumerable<Claim> claims, JwtTokenConfigDto tokenConfig)
    {
        var jwt = new JwtSecurityToken(
            issuer: tokenConfig.Issuer,
            audience: tokenConfig.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(tokenConfig.ExpiresDays),
            signingCredentials: new SigningCredentials(
                GetSymmetricSecurityKey(tokenConfig.Key),
                SecurityAlgorithms.HmacSha256
            )
        );

        var jwtToken = new JwtTokenDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(jwt),
        };
        return jwtToken;
    }
}
