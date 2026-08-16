using LF.AppDomain.Entities.Storage;

namespace LF.Application.Services.Storage;

public interface IStorageRepository
{
    Task<StorageObject> AddAsync(StorageObject storageObject, CancellationToken ct = default);
    Task<StorageObject?> GetByIdAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(StorageObject storageObject, CancellationToken ct = default);
}
