namespace LF.AppDomain.Entities.Groups;

// A cohort of students inside one course. Membership is deliberately not exclusive: a student may sit
// in several groups of the same course (e.g. a stream plus a lab subgroup).
public sealed class StudentGroup
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 1_000;

    private readonly List<StudentGroupMember> _members = [];

    private StudentGroup()
    {
    }

    public int Id { get; private set; }
    public int CourseId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<StudentGroupMember> Members => _members.AsReadOnly();

    public static StudentGroup Create(int courseId, string name, string? description, int createdByUserId, DateTime createdAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(courseId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(createdByUserId, 0);

        return new StudentGroup
        {
            CourseId = courseId,
            Name = NormalizeName(name),
            Description = NormalizeDescription(description),
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
        };
    }

    public bool Rename(string name)
    {
        var normalized = NormalizeName(name);
        if (normalized == Name)
            return false;

        Name = normalized;
        return true;
    }

    public bool UpdateDescription(string? description)
    {
        var normalized = NormalizeDescription(description);
        if (normalized == Description)
            return false;

        Description = normalized;
        return true;
    }

    public StudentGroupMember AddMember(int userId, int addedByUserId, DateTime addedAt)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("This student is already a member of the group.");

        var member = StudentGroupMember.Create(userId, addedByUserId, addedAt);
        _members.Add(member);
        return member;
    }

    public bool HasMember(int userId) => _members.Any(m => m.UserId == userId);

    public bool RemoveMember(int userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        return member is not null && _members.Remove(member);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name cannot be empty.", nameof(name));

        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
            throw new ArgumentException($"Group name cannot exceed {MaxNameLength} characters.", nameof(name));

        return trimmed;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var trimmed = description.Trim();
        if (trimmed.Length > MaxDescriptionLength)
            throw new ArgumentException($"Group description cannot exceed {MaxDescriptionLength} characters.", nameof(description));

        return trimmed;
    }
}
