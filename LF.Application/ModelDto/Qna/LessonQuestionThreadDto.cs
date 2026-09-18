namespace LF.Application.ModelDto.Qna;

public sealed class LessonQuestionThreadDto : LessonQuestionSummaryDto
{
    public IReadOnlyList<LessonQuestionMessageDto> Messages { get; init; } = [];
}
