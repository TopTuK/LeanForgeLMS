using LF.Application.ModelDto.Authentication;
using LF.Application.ModelDto.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace LF.Application.Services.Authentication
{
    public interface IAuthenticationService
    {
        Task<UserDto> AuthenticatePmiUserAsync(UserAuthentificationDto userAuthentification);
        Task<UserDto> AuthenticateGoogleUserAsync(UserAuthentificationDto userAuthentification);
        Task<UserDto> AuthenticateDevUserAsync(EnsureUserWithRoleDto userRequestDto);
    }
}
