using LF.AppDomain.Models.User.Enums;

namespace LF.Application.ModelDto.User;

public sealed class UpdateUserRoleDto
{
    public UserRole Role { get; set; }
}
