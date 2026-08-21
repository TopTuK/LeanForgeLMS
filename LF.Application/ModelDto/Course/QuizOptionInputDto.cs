namespace LF.Application.ModelDto.Course;

public sealed class QuizOptionInputDto
{
    public string Text { get; init; } = null!;
    public bool IsCorrect { get; init; }
    public int SortOrder { get; init; }
}
