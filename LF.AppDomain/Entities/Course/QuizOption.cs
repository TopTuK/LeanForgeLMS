namespace LF.AppDomain.Entities.Course;

public sealed class QuizOption
{
    private QuizOption()
    {
    }

    public int Id { get; private set; }
    public string Text { get; private set; } = null!;
    public bool IsCorrect { get; private set; }
    public int SortOrder { get; private set; }
    public int QuestionId { get; private set; }

    internal static QuizOption Create(string text, bool isCorrect, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Quiz option text cannot be empty.", nameof(text));

        return new QuizOption
        {
            Text = text.Trim(),
            IsCorrect = isCorrect,
            SortOrder = sortOrder,
        };
    }
}
