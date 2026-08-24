using LF.AppDomain.Entities.Storage;

namespace LF.AppDomain.Entities.Course;

public sealed class LessonPartFile
{
    private LessonPartFile()
    {
    }

    public int Id { get; private set; }
    public string FileName { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public int StorageObjectId { get; private set; }
    public StorageObject StorageObject { get; private set; } = null!;
    public int LessonPartId { get; private set; }

    internal static LessonPartFile Create(string fileName, int sortOrder, StorageObject storageObject)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));

        ArgumentNullException.ThrowIfNull(storageObject);

        return new LessonPartFile
        {
            FileName = fileName.Trim(),
            SortOrder = sortOrder,
            StorageObjectId = storageObject.Id,
            StorageObject = storageObject,
        };
    }
}
