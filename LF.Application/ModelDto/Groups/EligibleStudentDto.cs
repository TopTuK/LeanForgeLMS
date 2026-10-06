namespace LF.Application.ModelDto.Groups;

public sealed class EligibleStudentDto
{
    public int UserId { get; init; }
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string Email { get; init; } = null!;
}
