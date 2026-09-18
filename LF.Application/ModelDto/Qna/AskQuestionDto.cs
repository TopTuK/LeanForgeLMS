namespace LF.Application.ModelDto.Qna;

public sealed class AskQuestionDto
{
    public int LessonId { get; init; }
    public string Title { get; init; } = null!;
    public string Body { get; init; } = null!;
}
