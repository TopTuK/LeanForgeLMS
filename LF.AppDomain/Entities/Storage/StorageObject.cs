using LF.AppDomain.Models.Storage.Enums;

namespace LF.AppDomain.Entities.Storage;

public sealed class StorageObject
{
    private StorageObject()
    {
    }

    public int Id { get; private set; }
    public StorageObjectType ObjectType { get; private set; }
    public string ObjectKey { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    public int CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static StorageObject Create(
        StorageObjectType objectType,
        string objectKey,
        string contentType,
        long sizeBytes,
        int createdByUserId,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("Object key cannot be empty.", nameof(objectKey));

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type cannot be empty.", nameof(contentType));

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sizeBytes, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(createdByUserId, 0);

        return new StorageObject
        {
            ObjectType = objectType,
            ObjectKey = objectKey.Trim(),
            ContentType = contentType.Trim(),
            SizeBytes = sizeBytes,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
        };
    }
}
