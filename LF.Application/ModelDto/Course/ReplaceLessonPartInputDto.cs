using LF.AppDomain.Models.Course.Enums;

namespace LF.Application.ModelDto.Course;

public sealed class ReplaceLessonPartInputDto
{
    public LessonPartType PartType { get; init; }
    public string? Html { get; init; }
    public int? StorageObjectId { get; init; }
}
