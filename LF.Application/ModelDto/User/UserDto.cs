using LF.AppDomain.Models.User.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LF.Application.ModelDto.User
{
    public class UserDto
    {
        public int Id { get; init; }
        public string Email { get; init; } = null!;
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = string.Empty;
        public UserRole Role { get; init; } = UserRole.None;
        public DateTime CreatedAt { get; init; }
        public string? AvatarKey { get; init; }
        public string? Description { get; init; }
    }
}
