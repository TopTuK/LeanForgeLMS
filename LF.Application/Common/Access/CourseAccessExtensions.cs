using LF.AppDomain.Models.Course.Enums;
using LF.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LF.Application.Common.Access;

// The two course-scoped predicates the Q&A rules are built on. EnrollmentService inlines an
// equivalent enrollment check in a couple of places over in LF.CourseService; these live here
// because LF.WebApi needs them against the same shared database.
internal static class CourseAccessExtensions
{
    // Active enrollment only: a PendingPayment row must not buy access to the teaching staff.
    public static Task<bool> IsEnrolledAsync(this IAppDbContext dbContext, int courseId, int userId, CancellationToken cancellationToken) =>
        dbContext.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.CourseId == courseId && e.UserId == userId && e.Status == EnrollmentStatus.Active, cancellationToken);

    // Creator or assigned instructor. Admins bypass this entirely via the isAdmin flag callers pass.
    public static async Task<bool> IsTeachingStaffAsync(this IAppDbContext dbContext, int courseId, int userId, CancellationToken cancellationToken)
    {
        var isCreator = await dbContext.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Id == courseId && c.CreatedByUserId == userId, cancellationToken);

        return isCreator || await dbContext.CourseInstructors
            .AsNoTracking()
            .AnyAsync(i => i.CourseId == courseId && i.UserId == userId, cancellationToken);
    }
}
