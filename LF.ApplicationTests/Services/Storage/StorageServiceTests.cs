using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Storage.Enums;
using LF.Application.Common.Interfaces;
using LF.Application.Services.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LF.ApplicationTests.Services.Storage;

public class StorageServiceTests
{
    private static StorageService CreateService(
        out Mock<IFileStorageService> fileStorageServiceMock,
        out Mock<IStorageRepository> storageRepositoryMock)
    {
        fileStorageServiceMock = new Mock<IFileStorageService>();
        storageRepositoryMock = new Mock<IStorageRepository>();
        storageRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<StorageObject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StorageObject s, CancellationToken _) => s);

        return new StorageService(
            NullLogger<StorageService>.Instance, fileStorageServiceMock.Object, storageRepositoryMock.Object, TimeProvider.System);
    }

    [Fact]
    public async Task UploadImageAsync_UploadsToFileStorageThenPersistsMetadata()
    {
        // Arrange
        var service = CreateService(out var fileStorageServiceMock, out var storageRepositoryMock);
        using var content = new MemoryStream([1, 2, 3]);

        // Act
        var result = await service.UploadImageAsync(content, "image/png", 3, createdByUserId: 1);

        // Assert
        Assert.Equal(StorageObjectType.Image, result.ObjectType);
        Assert.StartsWith("images/", result.ObjectKey);
        Assert.EndsWith(".png", result.ObjectKey);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(1, result.CreatedByUserId);
        fileStorageServiceMock.Verify(f => f.UploadAsync(result.ObjectKey, content, "image/png", It.IsAny<CancellationToken>()), Times.Once);
        storageRepositoryMock.Verify(r => r.AddAsync(It.IsAny<StorageObject>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadImageAsync_RepositoryFails_DeletesUploadedObject()
    {
        // Arrange
        var service = CreateService(out var fileStorageServiceMock, out var storageRepositoryMock);
        storageRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<StorageObject>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));
        using var content = new MemoryStream([1, 2, 3]);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadImageAsync(content, "image/png", 3, createdByUserId: 1));
        fileStorageServiceMock.Verify(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var service = CreateService(out _, out var storageRepositoryMock);
        storageRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((StorageObject?)null);

        // Act
        var result = await service.GetAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_Existing_RemovesFromFileStorageAndRepository()
    {
        // Arrange
        var service = CreateService(out var fileStorageServiceMock, out var storageRepositoryMock);
        var storageObject = StorageObject.Create(StorageObjectType.Image, "images/a.png", "image/png", 100, 1, DateTime.UtcNow);
        storageRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(storageObject);

        // Act
        var result = await service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        fileStorageServiceMock.Verify(f => f.DeleteAsync("images/a.png", It.IsAny<CancellationToken>()), Times.Once);
        storageRepositoryMock.Verify(r => r.DeleteAsync(storageObject, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(out var fileStorageServiceMock, out var storageRepositoryMock);
        storageRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((StorageObject?)null);

        // Act
        var result = await service.DeleteAsync(999);

        // Assert
        Assert.False(result);
        fileStorageServiceMock.Verify(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
