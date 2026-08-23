using LF.AppDomain.Models.Course.Enums;

namespace LF.Application.ModelDto.Course;

public sealed class QuizQuestionDto
{
    public int Id { get; init; }
    public string Text { get; init; } = null!;
    public QuestionType QuestionType { get; init; }
    public int SortOrder { get; init; }
    public IReadOnlyList<QuizOptionDto> Options { get; init; } = [];
}
