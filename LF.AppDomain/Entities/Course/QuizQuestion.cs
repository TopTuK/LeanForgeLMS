using LF.AppDomain.Models.Course.Enums;

namespace LF.AppDomain.Entities.Course;

public sealed class QuizQuestion
{
    private readonly List<QuizOption> _options = [];

    private QuizQuestion()
    {
    }

    public int Id { get; private set; }
    public string Text { get; private set; } = null!;
    public QuestionType QuestionType { get; private set; }
    public int SortOrder { get; private set; }
    public int LessonPartId { get; private set; }
    public IReadOnlyList<QuizOption> Options => _options.AsReadOnly();

    public bool IsAnsweredCorrectly(IReadOnlyList<int> selectedOptionIds)
    {
        var correctOptionIds = _options.Where(o => o.IsCorrect).Select(o => o.Id).ToHashSet();
        return correctOptionIds.SetEquals(selectedOptionIds);
    }

    internal static QuizQuestion Create(string text, QuestionType questionType, int sortOrder, IReadOnlyList<QuizOptionInput> options)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Quiz question text cannot be empty.", nameof(text));

        ArgumentNullException.ThrowIfNull(options);
        if (options.Count < 2)
            throw new ArgumentException("Quiz questions require at least two options.", nameof(options));

        var correctCount = options.Count(o => o.IsCorrect);
        if (questionType == QuestionType.SingleChoice && correctCount != 1)
            throw new ArgumentException("Single-choice questions require exactly one correct option.", nameof(options));
        if (questionType == QuestionType.MultipleChoice && correctCount < 1)
            throw new ArgumentException("Multiple-choice questions require at least one correct option.", nameof(options));

        var question = new QuizQuestion
        {
            Text = text.Trim(),
            QuestionType = questionType,
            SortOrder = sortOrder,
        };

        for (var i = 0; i < options.Count; i++)
            question._options.Add(QuizOption.Create(options[i].Text, options[i].IsCorrect, i + 1));

        return question;
    }
}
