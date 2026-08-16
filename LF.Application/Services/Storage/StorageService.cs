using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Storage.Enums;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Storage;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Storage;

internal sealed class StorageService(
    ILogger<StorageService> logger,
    [FromKeyedServices("storage")] IFileStorageService fileStorageService,
    IStorageRepository storageRepository,
    TimeProvider timeProvider) : IStorageService
{
    private static readonly IReadOnlyDictionary<string, string> ImageExtensionsByContentType = new Dictionary<string, string>
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp",
    };

    public async Task<StorageObjectDto> UploadImageAsync(Stream content, string contentType, long sizeBytes, int createdByUserId, CancellationToken ct = default)
    {
        logger.LogInformation("StorageService::UploadImageAsync: called with ContentType={ContentType} SizeBytes={SizeBytes} CreatedByUserId={CreatedByUserId}",
            contentType, sizeBytes, createdByUserId);

        var extension = ImageExtensionsByContentType.GetValueOrDefault(contentType, string.Empty);
        var objectKey = $"images/{Guid.NewGuid():N}{extension}";

        await fileStorageService.UploadAsync(objectKey, content, contentType, ct);

        var storageObject = StorageObject.Create(
            StorageObjectType.Image, objectKey, contentType, sizeBytes, createdByUserId, timeProvider.GetUtcNow().UtcDateTime);

        try
        {
            var created = await storageRepository.AddAsync(storageObject, ct);
            return created.Adapt<StorageObjectDto>();
        }
        catch
        {
            await fileStorageService.DeleteAsync(objectKey, ct);
            throw;
        }
    }

    public async Task<StorageObjectDto?> GetAsync(int id, CancellationToken ct = default)
    {
        logger.LogInformation("StorageService::GetAsync: called with Id={StorageObjectId}", id);

        var storageObject = await storageRepository.GetByIdAsync(id, ct);
        return storageObject?.Adapt<StorageObjectDto>();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        logger.LogInformation("StorageService::DeleteAsync: called with Id={StorageObjectId}", id);

        var storageObject = await storageRepository.GetByIdAsync(id, ct);
        if (storageObject is null)
            return false;

        await fileStorageService.DeleteAsync(storageObject.ObjectKey, ct);
        await storageRepository.DeleteAsync(storageObject, ct);

        return true;
    }
}
