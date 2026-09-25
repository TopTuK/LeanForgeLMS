using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Email;
using LF.AppDomain.Entities.News;
using LF.AppDomain.Entities.Payment;
using LF.AppDomain.Entities.Qna;
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
    DbSet<StorageObject> StorageObjects { get; }
    DbSet<QuizAttempt> QuizAttempts { get; }
    DbSet<NewsPost> NewsPosts { get; }
    DbSet<NewsReadMarker> NewsReadMarkers { get; }
    DbSet<CourseInstructor> CourseInstructors { get; }
    DbSet<LessonQuestion> LessonQuestions { get; }
    DbSet<LessonQuestionReadMarker> LessonQuestionReadMarkers { get; }
    DbSet<EmailMessage> EmailMessages { get; }
    DbSet<CourseContentChange> CourseContentChanges { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
