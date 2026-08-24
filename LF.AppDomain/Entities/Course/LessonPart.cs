using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Course.Enums;

namespace LF.AppDomain.Entities.Course;

public sealed class LessonPart
{
    public const int DefaultQuizPassThresholdPercent = 60;

    private readonly List<QuizQuestion> _quizQuestions = [];
    private readonly List<LessonPartFile> _files = [];

    private LessonPart()
    {
    }

    public int Id { get; private set; }
    public LessonPartType PartType { get; private set; }
    public int SortOrder { get; private set; }
    public string? Html { get; private set; }
    public int? StorageObjectId { get; private set; }
    public StorageObject? StorageObject { get; private set; }
    public int LessonId { get; private set; }
    public IReadOnlyList<QuizQuestion> QuizQuestions => _quizQuestions.AsReadOnly();
    public int? QuizPassThresholdPercent { get; private set; }
    public IReadOnlyList<LessonPartFile> Files => _files.AsReadOnly();

    internal static LessonPart Create(
        LessonPartType partType,
        int sortOrder,
        string? html,
        StorageObject? storageObject,
        IReadOnlyList<QuizQuestionInput>? quizQuestions = null,
        int? quizPassThresholdPercent = null,
        IReadOnlyList<LessonPartFileInput>? files = null)
    {
        if (partType == LessonPartType.Text)
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new ArgumentException("Text lesson parts require non-empty content.", nameof(html));
        }
        else if (partType == LessonPartType.Quiz)
        {
            ArgumentNullException.ThrowIfNull(quizQuestions);
            if (quizQuestions.Count == 0)
                throw new ArgumentException("Quiz lesson parts require at least one question.", nameof(quizQuestions));

            if (quizPassThresholdPercent is < 1 or > 100)
                throw new ArgumentOutOfRangeException(nameof(quizPassThresholdPercent), "Quiz pass threshold must be between 1 and 100.");
        }
        else if (partType == LessonPartType.Files)
        {
            ArgumentNullException.ThrowIfNull(files);
            if (files.Count == 0)
                throw new ArgumentException("Files lesson parts require at least one file.", nameof(files));
        }
        else
        {
            ArgumentNullException.ThrowIfNull(storageObject);
        }

        var part = new LessonPart
        {
            PartType = partType,
            SortOrder = sortOrder,
            Html = partType == LessonPartType.Text ? html!.Trim() : null,
            StorageObjectId = storageObject?.Id,
            StorageObject = partType is LessonPartType.Quiz or LessonPartType.Files ? null : storageObject,
            QuizPassThresholdPercent = partType == LessonPartType.Quiz ? quizPassThresholdPercent ?? DefaultQuizPassThresholdPercent : null,
        };

        if (partType == LessonPartType.Quiz)
        {
            for (var i = 0; i < quizQuestions!.Count; i++)
                part._quizQuestions.Add(QuizQuestion.Create(quizQuestions[i].Text, quizQuestions[i].QuestionType, i + 1, quizQuestions[i].Options));
        }
        else if (partType == LessonPartType.Files)
        {
            for (var i = 0; i < files!.Count; i++)
                part._files.Add(LessonPartFile.Create(files[i].FileName, i + 1, files[i].StorageObject));
        }

        return part;
    }
}
