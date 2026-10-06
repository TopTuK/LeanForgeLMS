namespace LF.Application.ModelDto.Groups;

public sealed class StudentGroupMemberDto
{
    public int UserId { get; init; }
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;

    // Withheld (null) from student viewers: classmates see names, not contact details.
    public string? Email { get; init; }
    public DateTime AddedAt { get; init; }

    // False once the student's enrollment is no longer Active. Such members keep their row but lose
    // access; only staff ever see them listed.
    public bool IsEnrolled { get; init; }
}
