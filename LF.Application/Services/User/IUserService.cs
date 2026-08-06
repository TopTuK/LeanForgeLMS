using LF.Application.ModelDto.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace LF.Application.Services.User
{
    public interface IUserService
    {
        Task<UserDto> GetOrCreateUserAsync(GetOrCreateUserDto userRequestDto);
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<UserDto?> UpdateUserNameAsync(int id, UpdateUserNameDto dto);
    }
}
