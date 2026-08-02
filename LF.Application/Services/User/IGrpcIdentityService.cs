using LF.Application.ModelDto.Authentication;
using LF.Application.ModelDto.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace LF.Application.Services.User
{
    public interface IGrpcIdentityService
    {
        Task<UserDto> GetOrCreateUserAsync(UserAuthentificationDto userAuthentification);
    }
}
