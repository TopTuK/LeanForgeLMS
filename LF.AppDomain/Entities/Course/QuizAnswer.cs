namespace LF.AppDomain.Entities.Course;

public sealed class QuizAnswer
{
    private QuizAnswer()
    {
    }

    public int Id { get; private set; }
    public int QuizAttemptId { get; private set; }
    public int QuestionId { get; private set; }
    public int[] SelectedOptionIds { get; private set; } = [];

    internal static QuizAnswer Create(int questionId, IReadOnlyList<int> selectedOptionIds) => new()
    {
        QuestionId = questionId,
        SelectedOptionIds = [.. selectedOptionIds],
    };
}
