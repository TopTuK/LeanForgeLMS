namespace LF.Application.ModelDto.Course;

public sealed class QuizOptionDto
{
    public int Id { get; init; }
    public string Text { get; init; } = null!;
    public bool IsCorrect { get; init; }
    public int SortOrder { get; init; }
}
