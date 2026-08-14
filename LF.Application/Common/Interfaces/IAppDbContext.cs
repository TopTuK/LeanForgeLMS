using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace LF.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<DbUser> Users { get; }
    DbSet<Course> Courses { get; }
    DbSet<Category> Categories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
