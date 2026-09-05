using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Payment;
using LF.AppDomain.Entities.Platform;
using LF.AppDomain.Entities.User;
using LF.AppDomain.Models.Payment.Enums;
using LF.AppDomain.Models.User.Enums;
using LF.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LF.Infrastructure.Persistence.Seed;

public static class DatabaseInitializer
{
    private static readonly (string Name, bool IsDefault)[] DefaultCategories =
    [
        ("Common", true),
        ("Backend", false),
        ("Frontend", false),
        ("DevOps", false),
        ("Design", false),
        ("Career", false),
    ];

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

        foreach (var (name, isDefault) in DefaultCategories)
        {
            var exists = await dbContext.Categories.AnyAsync(c => c.Name == name);
            if (exists)
            {
                continue;
            }

            dbContext.Categories.Add(Category.Create(name, isDefault));
        }

        // Ships with student self-enrollment disabled; an admin flips it on from the Admin panel.
        if (!await dbContext.PlatformSettings.AnyAsync())
        {
            dbContext.PlatformSettings.Add(PlatformSettings.CreateDefault(DateTime.UtcNow));
        }

        await dbContext.SaveChangesAsync();

        await BackfillCoursePaymentsAsync(dbContext);
    }

    // Rebuilds the marketing payments ledger from settled orders so the report is complete even for
    // payments that predate the feature or whose webhook-time write was lost. Idempotent — mirrors
    // PaymentReportService.ReconcileAsync.
    private static async Task BackfillCoursePaymentsAsync(AppDbContext dbContext)
    {
        var recordedOrderIds = await dbContext.CoursePayments
            .Select(p => p.PaymentOrderId)
            .ToListAsync();

        var missingOrders = await dbContext.PaymentOrders
            .Where(o => o.Status == PaymentOrderStatus.Paid && !recordedOrderIds.Contains(o.Id))
            .ToListAsync();

        if (missingOrders.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var order in missingOrders)
        {
            var enrollment = await dbContext.Enrollments.FirstOrDefaultAsync(e => e.Id == order.EnrollmentId);
            if (enrollment is null)
            {
                continue;
            }

            var course = await dbContext.Courses.FirstOrDefaultAsync(c => c.Id == enrollment.CourseId);
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == enrollment.UserId);
            if (course is null || user is null)
            {
                continue;
            }

            var promoCode = enrollment.PromoCodeId is { } promoId
                ? await dbContext.PromoCodes.FirstOrDefaultAsync(p => p.Id == promoId)
                : null;

            dbContext.CoursePayments.Add(CoursePayment.Record(
                order.Id,
                order.EnrollmentId,
                enrollment.CourseId,
                enrollment.UserId,
                user.Email,
                $"{user.FirstName} {user.LastName}".Trim(),
                course.Title,
                order.Amount,
                promoCode?.Code,
                order.Provider,
                order.ProviderOperationId,
                order.PaidAt ?? now,
                now));
        }

        await dbContext.SaveChangesAsync();
    }
}
