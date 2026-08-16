using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Storage.Enums;

namespace LF.AppDomainTests.Entities.Storage;

public class StorageObjectTests
{
    private static StorageObject CreateStorageObject() =>
        StorageObject.Create(StorageObjectType.Image, "images/a.png", "image/png", 100, 1, DateTime.UtcNow);

    [Fact]
    public void Create_ValidArgs_SetsProperties()
    {
        // Act
        var storageObject = CreateStorageObject();

        // Assert
        Assert.Equal(StorageObjectType.Image, storageObject.ObjectType);
        Assert.Equal("images/a.png", storageObject.ObjectKey);
        Assert.Equal("image/png", storageObject.ContentType);
        Assert.Equal(100, storageObject.SizeBytes);
        Assert.Equal(1, storageObject.CreatedByUserId);
    }

    [Fact]
    public void Create_EmptyObjectKey_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            StorageObject.Create(StorageObjectType.Image, "  ", "image/png", 100, 1, DateTime.UtcNow));
    }

    [Fact]
    public void Create_EmptyContentType_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            StorageObject.Create(StorageObjectType.Image, "images/a.png", " ", 100, 1, DateTime.UtcNow));
    }

    [Fact]
    public void Create_NonPositiveSizeBytes_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StorageObject.Create(StorageObjectType.Image, "images/a.png", "image/png", 0, 1, DateTime.UtcNow));
    }

    [Fact]
    public void Create_NonPositiveCreatedByUserId_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StorageObject.Create(StorageObjectType.Image, "images/a.png", "image/png", 100, 0, DateTime.UtcNow));
    }
}
