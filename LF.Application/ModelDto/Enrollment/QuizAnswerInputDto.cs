namespace LF.Application.ModelDto.Enrollment;

public sealed class QuizAnswerInputDto
{
    public int QuestionId { get; init; }
    public IReadOnlyList<int> SelectedOptionIds { get; init; } = [];
}
