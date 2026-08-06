namespace LF.Application.ModelDto.User
{
    public sealed class UpdateUserNameDto
    {
        public string FirstName { get; set; } = null!;
        public string? LastName { get; set; } = null;
    }
}
