using LF.AppDomain.Models.Qna.Enums;

namespace LF.AppDomain.Entities.Qna;

// A private thread between one student and a course's teaching staff about one lesson.
public sealed class LessonQuestion
{
    public const int MaxTitleLength = 200;

    private readonly List<LessonQuestionMessage> _messages = [];

    private LessonQuestion()
    {
    }

    public int Id { get; private set; }

    // Denormalized from Lesson -> Chapter -> Course: Lesson carries only ChapterId, and every
    // authorization check on this thread is scoped by course, so storing it avoids a two-level join
    // on the hottest query in the feature.
    public int CourseId { get; private set; }
    public int LessonId { get; private set; }
    public int StudentUserId { get; private set; }
    public string Title { get; private set; } = null!;
    public LessonQuestionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastMessageAt { get; private set; }

    // Lets the unread query stay a single predicate over this table: a thread is unread for a viewer
    // when its last message is newer than their marker and they did not write it themselves.
    public int LastMessageAuthorUserId { get; private set; }

    public IReadOnlyList<LessonQuestionMessage> Messages => _messages.AsReadOnly();

    public static LessonQuestion Ask(int courseId, int lessonId, int studentUserId, string title, string body, DateTime askedAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(courseId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lessonId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(studentUserId, 0);

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Question title cannot be empty.", nameof(title));

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > MaxTitleLength)
            throw new ArgumentException($"Question title cannot exceed {MaxTitleLength} characters.", nameof(title));

        var question = new LessonQuestion
        {
            CourseId = courseId,
            LessonId = lessonId,
            StudentUserId = studentUserId,
            Title = trimmedTitle,
            Status = LessonQuestionStatus.Open,
            CreatedAt = askedAt,
        };

        question.AddMessage(studentUserId, QuestionAuthorRole.Student, body, askedAt);
        return question;
    }

    public LessonQuestionMessage AddMessage(int authorUserId, QuestionAuthorRole authorRole, string body, DateTime postedAt)
    {
        if (Status == LessonQuestionStatus.Closed)
            throw new InvalidOperationException("Cannot post to a closed question.");

        var message = LessonQuestionMessage.Create(authorUserId, authorRole, body, postedAt);
        _messages.Add(message);

        LastMessageAt = postedAt;
        LastMessageAuthorUserId = authorUserId;

        // A staff reply answers the thread; a follow-up from the student puts it back in the queue.
        Status = authorRole == QuestionAuthorRole.Student
            ? LessonQuestionStatus.Open
            : LessonQuestionStatus.Answered;

        return message;
    }

    public void Close()
    {
        if (Status == LessonQuestionStatus.Closed)
            throw new InvalidOperationException("Question is already closed.");

        Status = LessonQuestionStatus.Closed;
    }

    // Reopens to Answered when staff have already replied, so closing and reopening a resolved
    // thread doesn't push it back into the unanswered queue.
    public void Reopen()
    {
        if (Status != LessonQuestionStatus.Closed)
            throw new InvalidOperationException("Question is not closed.");

        Status = _messages.Any(m => m.AuthorRole != QuestionAuthorRole.Student)
            ? LessonQuestionStatus.Answered
            : LessonQuestionStatus.Open;
    }
}
