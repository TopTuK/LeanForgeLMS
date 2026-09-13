using LF.AppDomain.Entities.Storage;

namespace LF.AppDomain.Entities.News;

public sealed class NewsImage
{
    private NewsImage()
    {
    }

    public int Id { get; private set; }
    public int NewsPostId { get; private set; }
    public int StorageObjectId { get; private set; }
    public StorageObject StorageObject { get; private set; } = null!;
    public int SortOrder { get; private set; }

    internal static NewsImage Create(StorageObject storageObject, int sortOrder)
    {
        ArgumentNullException.ThrowIfNull(storageObject);

        return new NewsImage
        {
            StorageObjectId = storageObject.Id,
            StorageObject = storageObject,
            SortOrder = sortOrder,
        };
    }

    internal void MoveTo(int sortOrder) => SortOrder = sortOrder;
}
