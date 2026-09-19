namespace LF.Application.ModelDto.Qna;

// Totals for one inbox, driving the status filter chips and the course filter.
public sealed class LessonQuestionOverviewDto
{
    public int Total { get; init; }
    public int Open { get; init; }
    public int Answered { get; init; }
    public int Closed { get; init; }
    public int Unread { get; init; }
    public IReadOnlyList<LessonQuestionCourseCountDto> Courses { get; init; } = [];
}
