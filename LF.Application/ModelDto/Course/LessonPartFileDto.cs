namespace LF.Application.ModelDto.Course;

public sealed class LessonPartFileDto
{
    public int Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public int StorageObjectId { get; init; }
    public string StorageObjectKey { get; init; } = string.Empty;
    public string StorageObjectContentType { get; init; } = string.Empty;
    public long StorageObjectSizeBytes { get; init; }
}
