namespace LF.Application.ModelDto.Course;

public sealed class LessonPartFileInputDto
{
    public string FileName { get; init; } = string.Empty;
    public int StorageObjectId { get; init; }
}
