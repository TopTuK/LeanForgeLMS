using DomainCourse = LF.AppDomain.Entities.Course.Course;
using Lesson = LF.AppDomain.Entities.Course.Lesson;
using CourseContentChange = LF.AppDomain.Entities.Course.CourseContentChange;
using LF.AppDomain.Models.Course.Enums;
using LF.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Notifications;

internal sealed class CourseChangeTracker(
    ILogger<CourseChangeTracker> logger,
    IAppDbContext dbContext,
    TimeProvider timeProvider) : ICourseChangeTracker
{
    private readonly ILogger<CourseChangeTracker> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task TrackLessonChangeAsync(DomainCourse course, Lesson lesson, CourseContentChangeKind kind, CancellationToken cancellationToken = default)
    {
        if (!course.IsPublished)
            return;

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // A brand-new lesson has no id yet, so it cannot have a pending row to merge into.
        var pending = lesson.Id == 0
            ? null
            : await _dbContext.CourseContentChanges
                .FirstOrDefaultAsync(c => c.LessonId == lesson.Id && c.NotifiedAt == null, cancellationToken);

        if (pending is not null)
        {
            pending.Touch(kind, now);
        }
        else
        {
            _dbContext.CourseContentChanges.Add(CourseContentChange.Create(course.Id, lesson, kind, now));
        }

        _logger.LogInformation("CourseChangeTracker::TrackLessonChangeAsync: CourseId={CourseId} LessonId={LessonId} Kind={Kind} Merged={Merged}",
            course.Id, lesson.Id, kind, pending is not null);
    }
}
