using LF.AppDomain.Entities.Groups;
using LF.AppDomain.Models.Course.Enums;
using LF.Application.Common.Access;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Groups;

internal sealed class StudentGroupService(
    ILogger<StudentGroupService> logger,
    IAppDbContext dbContext,
    TimeProvider timeProvider) : IStudentGroupService
{
    private readonly ILogger<StudentGroupService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<IReadOnlyList<StudentGroupSummaryDto>?> ListForCourseAsync(int courseId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StudentGroupService::ListForCourseAsync: called with CourseId={CourseId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            courseId, actingUserId, isAdmin);

        if (!await CourseExistsAsync(courseId, cancellationToken))
            return null;

        await EnsureStaffAsync(courseId, actingUserId, isAdmin, cancellationToken);

        return await ToSummaries(_dbContext.StudentGroups.AsNoTracking().Where(g => g.CourseId == courseId), _dbContext.StaffCourseIds(actingUserId))
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<StudentGroupDetailDto?> GetAsync(int groupId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StudentGroupService::GetAsync: called with GroupId={GroupId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            groupId, actingUserId, isAdmin);

        var courseId = await GetGroupCourseIdAsync(groupId, cancellationToken);
        if (courseId is null)
            return null;

        var canManage = isAdmin || await _dbContext.IsTeachingStaffAsync(courseId.Value, actingUserId, cancellationToken);
        if (!canManage && !await _dbContext.MemberGroupIds(actingUserId).AnyAsync(id => id == groupId, cancellationToken))
            throw new GroupAuthorizationException("You do not have access to this group.");

        return await BuildDetailAsync(groupId, canManage, cancellationToken);
    }

    public async Task<StudentGroupDetailDto?> CreateAsync(int courseId, string name, string? description, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StudentGroupService::CreateAsync: called with CourseId={CourseId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            courseId, actingUserId, isAdmin);

        if (!await CourseExistsAsync(courseId, cancellationToken))
            return null;

        await EnsureStaffAsync(courseId, actingUserId, isAdmin, cancellationToken);

        var group = StudentGroup.Create(courseId, name, description, actingUserId, _timeProvider.GetUtcNow().UtcDateTime);
        await EnsureNameAvailableAsync(courseId, group.Name, exceptGroupId: null, cancellationToken);

        _dbContext.StudentGroups.Add(group);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildDetailAsync(group.Id, canManage: true, cancellationToken);
    }

    public async Task<StudentGroupDetailDto?> UpdateAsync(int groupId, string name, string? description, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StudentGroupService::UpdateAsync: called with GroupId={GroupId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            groupId, actingUserId, isAdmin);

        var group = await _dbContext.StudentGroups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
            return null;

        await EnsureStaffAsync(group.CourseId, actingUserId, isAdmin, cancellationToken);

        var renamed = group.Rename(name);
        if (renamed)
            await EnsureNameAvailableAsync(group.CourseId, group.Name, group.Id, cancellationToken);

        if (renamed | group.UpdateDescription(description))
            await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildDetailAsync(groupId, canManage: true, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int groupId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StudentGroupService::DeleteAsync: called with GroupId={GroupId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            groupId, actingUserId, isAdmin);

        var group = await _dbContext.StudentGroups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
            return false;

        await EnsureStaffAsync(group.CourseId, actingUserId, isAdmin, cancellationToken);

        // Members, chat history, read markers and lecture links all cascade with the group.
        _dbContext.StudentGroups.Remove(group);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<StudentGroupDetailDto?> AddMembersAsync(int groupId, IReadOnlyCollection<int> userIds, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StudentGroupService::AddMembersAsync: called with GroupId={GroupId} Count={Count} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            groupId, userIds.Count, actingUserId, isAdmin);

        var group = await LoadWithMembersAsync(groupId, cancellationToken);
        if (group is null)
            return null;

        await EnsureStaffAsync(group.CourseId, actingUserId, isAdmin, cancellationToken);

        var requested = userIds.Distinct().ToList();
        if (requested.Count == 0)
            throw new ArgumentException("At least one student must be selected.", nameof(userIds));

        var enrolled = await _dbContext.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == group.CourseId && e.Status == EnrollmentStatus.Active && requested.Contains(e.UserId))
            .Select(e => e.UserId)
            .ToListAsync(cancellationToken);

        if (enrolled.Count != requested.Count)
            throw new InvalidOperationException("Only students actively enrolled in this course can be added to its groups.");

        // Re-adding an existing member is a no-op rather than an error, so a picker can submit its
        // whole selection without diffing it first.
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var userId in requested.Where(id => !group.HasMember(id)))
            group.AddMember(userId, actingUserId, now);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildDetailAsync(groupId, canManage: true, cancellationToken);
    }

    public async Task<StudentGroupDetailDto?> RemoveMemberAsync(int groupId, int userId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StudentGroupService::RemoveMemberAsync: called with GroupId={GroupId} UserId={UserId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            groupId, userId, actingUserId, isAdmin);

        var group = await LoadWithMembersAsync(groupId, cancellationToken);
        if (group is null)
            return null;

        await EnsureStaffAsync(group.CourseId, actingUserId, isAdmin, cancellationToken);

        if (!group.RemoveMember(userId))
            return null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildDetailAsync(groupId, canManage: true, cancellationToken);
    }

    public async Task<IReadOnlyList<EligibleStudentDto>?> ListEligibleStudentsAsync(int courseId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StudentGroupService::ListEligibleStudentsAsync: called with CourseId={CourseId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            courseId, actingUserId, isAdmin);

        if (!await CourseExistsAsync(courseId, cancellationToken))
            return null;

        await EnsureStaffAsync(courseId, actingUserId, isAdmin, cancellationToken);

        return await _dbContext.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId && e.Status == EnrollmentStatus.Active)
            .Join(_dbContext.Users, e => e.UserId, u => u.Id, (e, u) => new EligibleStudentDto
            {
                UserId = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
            })
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudentGroupSummaryDto>> ListMyGroupsAsync(int actingUserId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("StudentGroupService::ListMyGroupsAsync: called with ActingUserId={ActingUserId}", actingUserId);

        var memberGroupIds = _dbContext.MemberGroupIds(actingUserId);
        var staffCourseIds = _dbContext.StaffCourseIds(actingUserId);

        // Includes the groups of courses the user teaches, so staff reach every chat they can post in
        // from the same page their unread badge points to.
        var groups = _dbContext.StudentGroups
            .AsNoTracking()
            .Where(g => memberGroupIds.Contains(g.Id) || staffCourseIds.Contains(g.CourseId));

        return await ToSummaries(groups, staffCourseIds)
            .OrderBy(g => g.CourseTitle)
            .ThenBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<StudentGroupSummaryDto> ToSummaries(IQueryable<StudentGroup> groups, IQueryable<int> staffCourseIds)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return groups.Join(_dbContext.Courses, g => g.CourseId, c => c.Id, (g, c) => new StudentGroupSummaryDto
        {
            Id = g.Id,
            CourseId = g.CourseId,
            CourseTitle = c.Title,
            Name = g.Name,
            Description = g.Description,
            MemberCount = g.Members.Count,
            CreatedAt = g.CreatedAt,
            NextLectureStartsAt = _dbContext.Lectures
                .Where(l => !l.IsCancelled && l.StartsAt >= now && l.Groups.Any(lg => lg.GroupId == g.Id))
                .Min(l => (DateTime?)l.StartsAt),
            IsTeaching = staffCourseIds.Contains(g.CourseId),
        });
    }

    private async Task<StudentGroupDetailDto?> BuildDetailAsync(int groupId, bool canManage, CancellationToken cancellationToken)
    {
        var header = await _dbContext.StudentGroups
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .Join(_dbContext.Courses, g => g.CourseId, c => c.Id, (g, c) => new
            {
                g.Id,
                g.CourseId,
                CourseTitle = c.Title,
                g.Name,
                g.Description,
                g.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (header is null)
            return null;

        var members = await _dbContext.StudentGroups
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .SelectMany(g => g.Members)
            .Join(_dbContext.Users, m => m.UserId, u => u.Id, (m, u) => new StudentGroupMemberDto
            {
                UserId = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = canManage ? u.Email : null,
                AddedAt = m.AddedAt,
                IsEnrolled = _dbContext.Enrollments.Any(e =>
                    e.CourseId == header.CourseId && e.UserId == u.Id && e.Status == EnrollmentStatus.Active),
            })
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync(cancellationToken);

        return new StudentGroupDetailDto
        {
            Id = header.Id,
            CourseId = header.CourseId,
            CourseTitle = header.CourseTitle,
            Name = header.Name,
            Description = header.Description,
            CreatedAt = header.CreatedAt,
            CanManage = canManage,
            Members = canManage ? members : [.. members.Where(m => m.IsEnrolled)],
        };
    }

    private Task<StudentGroup?> LoadWithMembersAsync(int groupId, CancellationToken cancellationToken) =>
        _dbContext.StudentGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

    private Task<bool> CourseExistsAsync(int courseId, CancellationToken cancellationToken) =>
        _dbContext.Courses.AsNoTracking().AnyAsync(c => c.Id == courseId, cancellationToken);

    private async Task<int?> GetGroupCourseIdAsync(int groupId, CancellationToken cancellationToken)
    {
        var courseId = await _dbContext.StudentGroups
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .Select(g => g.CourseId)
            .FirstOrDefaultAsync(cancellationToken);

        return courseId == 0 ? null : courseId;
    }

    // The unique (CourseId, Name) index would reject a duplicate anyway; checking first turns that
    // into a readable 409 instead of a DbUpdateException.
    private async Task EnsureNameAvailableAsync(int courseId, string name, int? exceptGroupId, CancellationToken cancellationToken)
    {
        var taken = await _dbContext.StudentGroups
            .AsNoTracking()
            .AnyAsync(g => g.CourseId == courseId && g.Name == name && g.Id != exceptGroupId, cancellationToken);

        if (taken)
            throw new InvalidOperationException("A group with this name already exists in the course.");
    }

    private async Task EnsureStaffAsync(int courseId, int actingUserId, bool isAdmin, CancellationToken cancellationToken)
    {
        if (isAdmin)
            return;

        if (!await _dbContext.IsTeachingStaffAsync(courseId, actingUserId, cancellationToken))
            throw new GroupAuthorizationException("You do not teach this course.");
    }
}
