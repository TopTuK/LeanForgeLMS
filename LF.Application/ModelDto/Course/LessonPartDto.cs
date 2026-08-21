using LF.AppDomain.Models.Course.Enums;

namespace LF.Application.ModelDto.Course;

public sealed class LessonPartDto
{
    public int Id { get; init; }
    public LessonPartType PartType { get; init; }
    public int SortOrder { get; init; }
    public string? Html { get; init; }
    public int? StorageObjectId { get; init; }
    public string? StorageObjectKey { get; init; }
    public string? StorageObjectContentType { get; init; }
    public IReadOnlyList<QuizQuestionDto> QuizQuestions { get; init; } = [];
    public int? QuizPassThresholdPercent { get; init; }
}
