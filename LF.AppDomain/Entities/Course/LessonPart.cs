using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Course.Enums;

namespace LF.AppDomain.Entities.Course;

public sealed class LessonPart
{
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

    internal static LessonPart Create(LessonPartType partType, int sortOrder, string? html, StorageObject? storageObject)
    {
        if (partType == LessonPartType.Text)
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new ArgumentException("Text lesson parts require non-empty content.", nameof(html));
        }
        else
        {
            ArgumentNullException.ThrowIfNull(storageObject);
        }

        return new LessonPart
        {
            PartType = partType,
            SortOrder = sortOrder,
            Html = partType == LessonPartType.Text ? html!.Trim() : null,
            StorageObjectId = storageObject?.Id,
            StorageObject = storageObject,
        };
    }
}
