using LF.Application.ModelDto.Authentication;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace LF.Application.Services.Authentication
{
    public interface ITokenService
    {
        JwtTokenDto CreateWebJwtToken(IEnumerable<Claim> claims, JwtTokenConfigDto tokenConfig);
    }
}
