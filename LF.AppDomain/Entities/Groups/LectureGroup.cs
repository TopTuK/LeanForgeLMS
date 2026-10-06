namespace LF.AppDomain.Entities.Groups;

public sealed class LectureGroup
{
    private LectureGroup()
    {
    }

    public int LectureId { get; private set; }
    public int GroupId { get; private set; }

    internal static LectureGroup Create(int groupId) => new() { GroupId = groupId };
}
