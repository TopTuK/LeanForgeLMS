namespace LF.Application.ModelDto.Qna;

public sealed class LessonQuestionCourseCountDto
{
    public int CourseId { get; init; }
    public string CourseTitle { get; init; } = null!;
    public int Count { get; init; }
}
