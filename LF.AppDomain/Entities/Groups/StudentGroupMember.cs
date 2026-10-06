namespace LF.AppDomain.Entities.Groups;

// Membership alone doesn't grant access: the student also needs an Active enrollment in the group's
// course, which the application layer checks on every read so a removed enrollment needs no cleanup here.
public sealed class StudentGroupMember
{
    private StudentGroupMember()
    {
    }

    public int Id { get; private set; }
    public int GroupId { get; private set; }
    public int UserId { get; private set; }
    public int AddedByUserId { get; private set; }
    public DateTime AddedAt { get; private set; }

    internal static StudentGroupMember Create(int userId, int addedByUserId, DateTime addedAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(userId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(addedByUserId, 0);

        return new StudentGroupMember
        {
            UserId = userId,
            AddedByUserId = addedByUserId,
            AddedAt = addedAt,
        };
    }
}
