using LF.AppDomain.Models.Course.Enums;

namespace LF.AppDomain.Entities.Course;

public sealed class QuizAttempt
{
    private readonly List<QuizAnswer> _answers = [];

    private QuizAttempt()
    {
    }

    public int Id { get; private set; }
    public int EnrollmentId { get; private set; }
    public int LessonId { get; private set; }
    public int ScorePercent { get; private set; }
    public bool Passed { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }
    public IReadOnlyList<QuizAnswer> Answers => _answers.AsReadOnly();

    public static QuizAttempt Grade(
        LessonPart quizPart,
        IReadOnlyDictionary<int, IReadOnlyList<int>> selectedOptionIdsByQuestionId,
        int enrollmentId,
        int lessonId,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(quizPart);
        ArgumentNullException.ThrowIfNull(selectedOptionIdsByQuestionId);

        if (quizPart.PartType != LessonPartType.Quiz)
            throw new ArgumentException("Only quiz lesson parts can be graded.", nameof(quizPart));

        var attempt = new QuizAttempt
        {
            EnrollmentId = enrollmentId,
            LessonId = lessonId,
            SubmittedAtUtc = nowUtc,
        };

        var correctCount = 0;
        foreach (var question in quizPart.QuizQuestions)
        {
            var selected = selectedOptionIdsByQuestionId.TryGetValue(question.Id, out var value) ? value : [];
            if (question.IsAnsweredCorrectly(selected))
                correctCount++;

            attempt._answers.Add(QuizAnswer.Create(question.Id, selected));
        }

        var totalQuestions = quizPart.QuizQuestions.Count;
        attempt.ScorePercent = totalQuestions == 0 ? 0 : (int)Math.Round(correctCount * 100.0 / totalQuestions, MidpointRounding.AwayFromZero);
        attempt.Passed = attempt.ScorePercent >= (quizPart.QuizPassThresholdPercent ?? LessonPart.DefaultQuizPassThresholdPercent);

        return attempt;
    }
}
