using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Course.Enums;

namespace LF.AppDomain.Entities.Course;

public sealed record LessonPartInput(
    LessonPartType PartType,
    string? Html,
    StorageObject? StorageObject,
    IReadOnlyList<QuizQuestionInput>? QuizQuestions = null,
    int? QuizPassThresholdPercent = null,
    IReadOnlyList<LessonPartFileInput>? Files = null);
