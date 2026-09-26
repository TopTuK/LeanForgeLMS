using DomainCourse = LF.AppDomain.Entities.Course.Course;
using Lesson = LF.AppDomain.Entities.Course.Lesson;
using LF.AppDomain.Models.Course.Enums;

namespace LF.Application.Services.Notifications;

public interface ICourseChangeTracker
{
    // Stages a pending change on the shared DbContext WITHOUT saving; the caller's SaveChangesAsync
    // commits it with the lesson edit. No-op for unpublished courses (nobody can be enrolled to see it).
    Task TrackLessonChangeAsync(DomainCourse course, Lesson lesson, CourseContentChangeKind kind, CancellationToken cancellationToken = default);
}
