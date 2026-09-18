using LF.AppDomain.Models.Qna.Enums;

namespace LF.Application.ModelDto.Qna;

public sealed class LessonQuestionMessageDto
{
    public int Id { get; init; }
    public int AuthorUserId { get; init; }
    public string AuthorName { get; init; } = null!;
    public QuestionAuthorRole AuthorRole { get; init; }

    // Null when the message was deleted by an admin: the row stays so the thread keeps its shape.
    public string? Body { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsDeleted { get; init; }
}
