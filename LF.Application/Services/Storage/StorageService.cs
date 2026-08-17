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

    private static readonly IReadOnlyDictionary<string, string> VideoExtensionsByContentType = new Dictionary<string, string>
    {
        ["video/mp4"] = ".mp4",
        ["video/webm"] = ".webm",
    };

    private static readonly IReadOnlyDictionary<string, string> AudioExtensionsByContentType = new Dictionary<string, string>
    {
        ["audio/mpeg"] = ".mp3",
        ["audio/wav"] = ".wav",
        ["audio/ogg"] = ".ogg",
        ["audio/webm"] = ".webm",
    };

    private static readonly IReadOnlyDictionary<StorageObjectType, IReadOnlyDictionary<string, string>> ExtensionsByObjectType =
        new Dictionary<StorageObjectType, IReadOnlyDictionary<string, string>>
        {
            [StorageObjectType.Image] = ImageExtensionsByContentType,
            [StorageObjectType.Video] = VideoExtensionsByContentType,
            [StorageObjectType.Audio] = AudioExtensionsByContentType,
        };

    private static readonly IReadOnlyDictionary<StorageObjectType, string> KeyPrefixByObjectType = new Dictionary<StorageObjectType, string>
    {
        [StorageObjectType.Image] = "images",
        [StorageObjectType.Video] = "videos",
        [StorageObjectType.Audio] = "audio",
    };

    public Task<StorageObjectDto> UploadImageAsync(Stream content, string contentType, long sizeBytes, int createdByUserId, CancellationToken ct = default) =>
        UploadMediaAsync(StorageObjectType.Image, content, contentType, sizeBytes, createdByUserId, ct);

    public async Task<StorageObjectDto> UploadMediaAsync(
        StorageObjectType objectType, Stream content, string contentType, long sizeBytes, int createdByUserId, CancellationToken ct = default)
    {
        logger.LogInformation("StorageService::UploadMediaAsync: called with ObjectType={ObjectType} ContentType={ContentType} SizeBytes={SizeBytes} CreatedByUserId={CreatedByUserId}",
            objectType, contentType, sizeBytes, createdByUserId);

        var extension = ExtensionsByObjectType[objectType].GetValueOrDefault(contentType, string.Empty);
        var objectKey = $"{KeyPrefixByObjectType[objectType]}/{Guid.NewGuid():N}{extension}";

        await fileStorageService.UploadAsync(objectKey, content, contentType, ct);

        var storageObject = StorageObject.Create(
            objectType, objectKey, contentType, sizeBytes, createdByUserId, timeProvider.GetUtcNow().UtcDateTime);

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
