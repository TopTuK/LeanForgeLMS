namespace LF.Application.ModelDto.Enrollment;

public sealed class QuizQuestionResultDto
{
    public int QuestionId { get; init; }
    public bool IsCorrect { get; init; }
    public IReadOnlyList<int> CorrectOptionIds { get; init; } = [];
}
