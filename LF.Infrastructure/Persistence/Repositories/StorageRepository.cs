using LF.AppDomain.Entities.Storage;
using LF.Application.Common.Interfaces;
using LF.Application.Services.Storage;
using Microsoft.EntityFrameworkCore;

namespace LF.Infrastructure.Persistence.Repositories;

internal sealed class StorageRepository(IAppDbContext dbContext) : IStorageRepository
{
    public async Task<StorageObject> AddAsync(StorageObject storageObject, CancellationToken ct = default)
    {
        dbContext.StorageObjects.Add(storageObject);
        await dbContext.SaveChangesAsync(ct);

        return storageObject;
    }

    public Task<StorageObject?> GetByIdAsync(int id, CancellationToken ct = default) =>
        dbContext.StorageObjects.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task DeleteAsync(StorageObject storageObject, CancellationToken ct = default)
    {
        dbContext.StorageObjects.Remove(storageObject);
        await dbContext.SaveChangesAsync(ct);
    }
}
