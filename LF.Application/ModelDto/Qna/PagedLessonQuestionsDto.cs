namespace LF.Application.ModelDto.Qna;

public sealed class PagedLessonQuestionsDto
{
    public IReadOnlyList<LessonQuestionSummaryDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
}
