using LF.AppDomain.Models.User.Enums;

namespace LF.AppDomain.Entities.User;

public sealed class DbUser
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.None;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
