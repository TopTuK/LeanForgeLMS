using LF.AppDomain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace LF.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<DbUser> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
