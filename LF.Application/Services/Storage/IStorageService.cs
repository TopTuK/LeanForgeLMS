using LF.AppDomain.Models.Storage.Enums;
using LF.Application.ModelDto.Storage;

namespace LF.Application.Services.Storage;

public interface IStorageService
{
    Task<StorageObjectDto> UploadImageAsync(Stream content, string contentType, long sizeBytes, int createdByUserId, CancellationToken ct = default);
    Task<StorageObjectDto> UploadMediaAsync(StorageObjectType objectType, Stream content, string contentType, long sizeBytes, int createdByUserId, CancellationToken ct = default);
    Task<StorageObjectDto?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
