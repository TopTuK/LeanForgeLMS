using LF.AppDomain.Models.Qna.Enums;

namespace LF.AppDomain.Entities.Qna;

// Bodies are plain text, never HTML: students are the only untrusted authors in the app, and the
// Q&A surface deliberately keeps them off the sanitizer/rich-text path the lesson and news bodies use.
public sealed class LessonQuestionMessage
{
    public const int MaxBodyLength = 5_000;

    private LessonQuestionMessage()
    {
    }

    public int Id { get; private set; }
    public int LessonQuestionId { get; private set; }
    public int AuthorUserId { get; private set; }
    public QuestionAuthorRole AuthorRole { get; private set; }
    public string Body { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedByUserId { get; private set; }

    // Only reachable through LessonQuestion.Ask/AddMessage, so a message can never exist
    // outside a thread or skip the thread's own status bookkeeping.
    internal static LessonQuestionMessage Create(int authorUserId, QuestionAuthorRole authorRole, string body, DateTime createdAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(authorUserId, 0);

        if (!Enum.IsDefined(authorRole))
            throw new ArgumentException("Unknown question author role.", nameof(authorRole));

        return new LessonQuestionMessage
        {
            AuthorUserId = authorUserId,
            AuthorRole = authorRole,
            Body = NormalizeBody(body),
            CreatedAt = createdAt,
        };
    }

    // Deletion is soft so an admin removing an abusive message doesn't punch a hole in the middle
    // of a conversation — the row stays, the body is withheld at projection time.
    public void SoftDelete(int deletedByUserId, DateTime deletedAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(deletedByUserId, 0);

        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedByUserId = deletedByUserId;
        DeletedAt = deletedAt;
    }

    internal static string NormalizeBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Message body cannot be empty.", nameof(body));

        var trimmed = body.Trim();
        if (trimmed.Length > MaxBodyLength)
            throw new ArgumentException($"Message body cannot exceed {MaxBodyLength} characters.", nameof(body));

        return trimmed;
    }
}
