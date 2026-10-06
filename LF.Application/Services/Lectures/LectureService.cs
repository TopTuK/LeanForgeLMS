using LF.AppDomain.Entities.Groups;
using LF.Application.Common.Access;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Lectures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Lectures;

internal sealed class LectureService(
    ILogger<LectureService> logger,
    IAppDbContext dbContext,
    TimeProvider timeProvider) : ILectureService
{
    // Bounds the schedule query so a client can't ask for every lecture ever held in one request.
    public static readonly TimeSpan MaxScheduleWindow = TimeSpan.FromDays(366);

    private readonly ILogger<LectureService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<IReadOnlyList<LectureDto>?> ListForCourseAsync(int courseId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LectureService::ListForCourseAsync: called with CourseId={CourseId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            courseId, actingUserId, isAdmin);

        if (!await _dbContext.Courses.AsNoTracking().AnyAsync(c => c.Id == courseId, cancellationToken))
            return null;

        await EnsureStaffAsync(courseId, actingUserId, isAdmin, cancellationToken);

        var lectures = await _dbContext.Lectures
            .AsNoTracking()
            .Include(l => l.Groups)
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.StartsAt)
            .ToListAsync(cancellationToken);

        return await ToDtosAsync(lectures, teachingCourseIds: [courseId], cancellationToken);
    }

    public async Task<LectureDto?> ScheduleAsync(int courseId, LectureInputDto input, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LectureService::ScheduleAsync: called with CourseId={CourseId} StartsAt={StartsAt} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            courseId, input.StartsAt, actingUserId, isAdmin);

        if (!await _dbContext.Courses.AsNoTracking().AnyAsync(c => c.Id == courseId, cancellationToken))
            return null;

        await EnsureStaffAsync(courseId, actingUserId, isAdmin, cancellationToken);
        await EnsureGroupsBelongToCourseAsync(courseId, input.GroupIds, cancellationToken);

        var lecture = Lecture.Schedule(
            courseId,
            input.Title,
            input.Description,
            ToUtc(input.StartsAt),
            input.DurationMinutes,
            input.MeetingUrl,
            input.GroupIds.ToList(),
            actingUserId,
            _timeProvider.GetUtcNow().UtcDateTime);

        _dbContext.Lectures.Add(lecture);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await ToDtosAsync([lecture], teachingCourseIds: [courseId], cancellationToken))[0];
    }

    public async Task<LectureDto?> UpdateAsync(int lectureId, LectureInputDto input, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LectureService::UpdateAsync: called with LectureId={LectureId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            lectureId, actingUserId, isAdmin);

        var lecture = await LoadForUpdateAsync(lectureId, cancellationToken);
        if (lecture is null)
            return null;

        await EnsureStaffAsync(lecture.CourseId, actingUserId, isAdmin, cancellationToken);

        if (lecture.IsCancelled)
            throw new InvalidOperationException("A cancelled lecture cannot be edited.");

        await EnsureGroupsBelongToCourseAsync(lecture.CourseId, input.GroupIds, cancellationToken);

        var changed = lecture.UpdateDetails(input.Title, input.Description, input.MeetingUrl);
        changed |= lecture.Reschedule(ToUtc(input.StartsAt), input.DurationMinutes);
        changed |= lecture.AssignGroups(input.GroupIds.ToList());

        if (changed)
            await _dbContext.SaveChangesAsync(cancellationToken);

        return (await ToDtosAsync([lecture], teachingCourseIds: [lecture.CourseId], cancellationToken))[0];
    }

    public async Task<LectureDto?> CancelAsync(int lectureId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LectureService::CancelAsync: called with LectureId={LectureId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            lectureId, actingUserId, isAdmin);

        var lecture = await LoadForUpdateAsync(lectureId, cancellationToken);
        if (lecture is null)
            return null;

        await EnsureStaffAsync(lecture.CourseId, actingUserId, isAdmin, cancellationToken);

        lecture.Cancel();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await ToDtosAsync([lecture], teachingCourseIds: [lecture.CourseId], cancellationToken))[0];
    }

    public async Task<bool> DeleteAsync(int lectureId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LectureService::DeleteAsync: called with LectureId={LectureId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            lectureId, actingUserId, isAdmin);

        var lecture = await _dbContext.Lectures.FirstOrDefaultAsync(l => l.Id == lectureId, cancellationToken);
        if (lecture is null)
            return false;

        await EnsureStaffAsync(lecture.CourseId, actingUserId, isAdmin, cancellationToken);

        _dbContext.Lectures.Remove(lecture);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<LectureDto>> ListMyScheduleAsync(int actingUserId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LectureService::ListMyScheduleAsync: called with ActingUserId={ActingUserId} From={From} To={To}",
            actingUserId, from, to);

        var fromUtc = ToUtc(from);
        var toUtc = ToUtc(to);
        if (toUtc <= fromUtc)
            throw new ArgumentException("The end of the range must be after its start.", nameof(to));

        if (toUtc - fromUtc > MaxScheduleWindow)
            throw new ArgumentException($"The range cannot exceed {MaxScheduleWindow.TotalDays} days.", nameof(to));

        var memberGroupIds = _dbContext.MemberGroupIds(actingUserId);
        var staffCourseIds = _dbContext.StaffCourseIds(actingUserId);

        var lectures = await _dbContext.Lectures
            .AsNoTracking()
            .Include(l => l.Groups)
            .Where(l => l.StartsAt >= fromUtc && l.StartsAt < toUtc)
            .Where(l => staffCourseIds.Contains(l.CourseId) || l.Groups.Any(g => memberGroupIds.Contains(g.GroupId)))
            .OrderBy(l => l.StartsAt)
            .ToListAsync(cancellationToken);

        var teachingCourseIds = await staffCourseIds.ToListAsync(cancellationToken);
        return await ToDtosAsync(lectures, [.. teachingCourseIds], cancellationToken);
    }

    private async Task<IReadOnlyList<LectureDto>> ToDtosAsync(IReadOnlyList<Lecture> lectures, HashSet<int> teachingCourseIds, CancellationToken cancellationToken)
    {
        if (lectures.Count == 0)
            return [];

        var courseIds = lectures.Select(l => l.CourseId).Distinct().ToList();
        var groupIds = lectures.SelectMany(l => l.Groups).Select(g => g.GroupId).Distinct().ToList();

        var courseTitles = await _dbContext.Courses
            .AsNoTracking()
            .Where(c => courseIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Title })
            .ToDictionaryAsync(c => c.Id, c => c.Title, cancellationToken);

        var groupNames = await _dbContext.StudentGroups
            .AsNoTracking()
            .Where(g => groupIds.Contains(g.Id))
            .Select(g => new { g.Id, g.Name })
            .ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken);

        return
        [
            .. lectures.Select(l => new LectureDto
            {
                Id = l.Id,
                CourseId = l.CourseId,
                CourseTitle = courseTitles.GetValueOrDefault(l.CourseId, string.Empty),
                Title = l.Title,
                Description = l.Description,
                StartsAt = l.StartsAt,
                DurationMinutes = l.DurationMinutes,
                MeetingUrl = l.IsCancelled ? null : l.MeetingUrl,
                IsCancelled = l.IsCancelled,
                Groups =
                [
                    .. l.Groups
                        .Where(g => groupNames.ContainsKey(g.GroupId))
                        .Select(g => new LectureGroupRefDto { Id = g.GroupId, Name = groupNames[g.GroupId] })
                        .OrderBy(g => g.Name),
                ],
                IsTeaching = teachingCourseIds.Contains(l.CourseId),
            }),
        ];
    }

    private Task<Lecture?> LoadForUpdateAsync(int lectureId, CancellationToken cancellationToken) =>
        _dbContext.Lectures
            .Include(l => l.Groups)
            .FirstOrDefaultAsync(l => l.Id == lectureId, cancellationToken);

    private async Task EnsureGroupsBelongToCourseAsync(int courseId, IReadOnlyList<int> groupIds, CancellationToken cancellationToken)
    {
        var distinct = groupIds.Distinct().ToList();
        var matching = await _dbContext.StudentGroups
            .AsNoTracking()
            .CountAsync(g => g.CourseId == courseId && distinct.Contains(g.Id), cancellationToken);

        if (matching != distinct.Count)
            throw new ArgumentException("Every group must belong to the lecture's course.", nameof(groupIds));
    }

    private async Task EnsureStaffAsync(int courseId, int actingUserId, bool isAdmin, CancellationToken cancellationToken)
    {
        if (isAdmin)
            return;

        if (!await _dbContext.IsTeachingStaffAsync(courseId, actingUserId, cancellationToken))
            throw new GroupAuthorizationException("You do not teach this course.");
    }

    // JSON binding yields Utc for "...Z" strings but Local/Unspecified for offset-less ones; the
    // domain insists on UTC, so normalize here rather than surface a confusing error.
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
