using LF.AppDomain.Entities.User;
using LF.AppDomain.Models.User.Enums;
using LF.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LF.Infrastructure.Persistence.Seed;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        var dbContext = scopedServices.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        var defaultAdmins = scopedServices.GetRequiredService<IOptions<List<DefaultAdminEntry>>>().Value;
        foreach (var admin in defaultAdmins)
        {
            var exists = await dbContext.Users.AnyAsync(u => u.Email == admin.Email);
            if (exists)
            {
                continue;
            }

            dbContext.Users.Add(new DbUser
            {
                Email = admin.Email,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                Role = UserRole.Admin,
            });
        }

        await dbContext.SaveChangesAsync();
    }
}
