namespace LF.AppDomain.Entities.Groups;

// Plain text only, never HTML, for the same reason as Q&A messages: students are untrusted authors.
public sealed class GroupChatMessage
{
    public const int MaxBodyLength = 4_000;

    private GroupChatMessage()
    {
    }

    public int Id { get; private set; }
    public int GroupId { get; private set; }
    public int AuthorUserId { get; private set; }
    public string Body { get; private set; } = null!;
    public DateTime SentAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedByUserId { get; private set; }

    public static GroupChatMessage Post(int groupId, int authorUserId, string body, DateTime sentAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(groupId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(authorUserId, 0);

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Message cannot be empty.", nameof(body));

        var trimmed = body.Trim();
        if (trimmed.Length > MaxBodyLength)
            throw new ArgumentException($"Message cannot exceed {MaxBodyLength} characters.", nameof(body));

        return new GroupChatMessage
        {
            GroupId = groupId,
            AuthorUserId = authorUserId,
            Body = trimmed,
            SentAt = sentAt,
        };
    }

    // Soft so the conversation keeps its shape; the body is withheld at projection time.
    public bool SoftDelete(int deletedByUserId, DateTime deletedAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(deletedByUserId, 0);

        if (IsDeleted)
            return false;

        IsDeleted = true;
        DeletedByUserId = deletedByUserId;
        DeletedAt = deletedAt;
        return true;
    }
}
