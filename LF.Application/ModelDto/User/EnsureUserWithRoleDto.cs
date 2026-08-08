using LF.AppDomain.Models.User.Enums;

namespace LF.Application.ModelDto.User;

public sealed class EnsureUserWithRoleDto
{
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? LastName { get; set; }
    public UserRole Role { get; set; }
}
