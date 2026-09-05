using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Payment;
using LF.AppDomain.Entities.Platform;
using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace LF.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<DbUser> Users { get; }
    DbSet<Course> Courses { get; }
    DbSet<Category> Categories { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<PromoCode> PromoCodes { get; }
    DbSet<PaymentOrder> PaymentOrders { get; }
    DbSet<CoursePayment> CoursePayments { get; }
    DbSet<PlatformSettings> PlatformSettings { get; }
    DbSet<StorageObject> StorageObjects { get; }
    DbSet<QuizAttempt> QuizAttempts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
