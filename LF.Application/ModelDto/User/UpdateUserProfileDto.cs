namespace LF.Application.ModelDto.User
{
    public sealed class UpdateUserProfileDto
    {
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; } = null;
        public string? Description { get; set; } = null;
    }
}
