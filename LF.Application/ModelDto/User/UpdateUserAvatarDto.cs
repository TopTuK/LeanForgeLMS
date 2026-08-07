namespace LF.Application.ModelDto.User
{
    public sealed class UpdateUserAvatarDto
    {
        // Null clears the avatar, reverting the user to the default image.
        public string? AvatarKey { get; set; }
    }
}
