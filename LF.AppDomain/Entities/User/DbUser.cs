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
    public string? AvatarKey { get; set; }

    /// <returns>true if the name actually changed; false if the given values already match.</returns>
    public bool UpdateName(string firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));

        var normalizedLastName = lastName ?? string.Empty;
        if (FirstName == firstName && LastName == normalizedLastName)
            return false;

        FirstName = firstName;
        LastName = normalizedLastName;
        return true;
    }

    /// <returns>true if the role actually changed; false if it already matched.</returns>
    public bool EnsureRole(UserRole role)
    {
        if (Role == role)
            return false;

        Role = role;
        return true;
    }

    public void SetAvatar(string avatarKey)
    {
        if (string.IsNullOrWhiteSpace(avatarKey))
            throw new ArgumentException("Avatar key cannot be empty.", nameof(avatarKey));

        AvatarKey = avatarKey;
    }

    public void ClearAvatar() => AvatarKey = null;
}
