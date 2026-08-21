namespace LF.Application.ModelDto.Enrollment;

public sealed class QuizAttemptResultDto
{
    public int ScorePercent { get; init; }
    public bool Passed { get; init; }
    public IReadOnlyList<QuizQuestionResultDto> Questions { get; init; } = [];
}
