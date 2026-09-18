using LF.AppDomain.Entities.Course;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Qna;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.CourseTeaching;

internal sealed class CourseTeachingTeamService(
    ILogger<CourseTeachingTeamService> logger,
    IAppDbContext dbContext,
    TimeProvider timeProvider) : ICourseTeachingTeamService
{
    private readonly ILogger<CourseTeachingTeamService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<IReadOnlyList<CourseInstructorDto>?> ListAsync(int courseId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CourseTeachingTeamService::ListAsync: called with CourseId={CourseId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            courseId, actingUserId, isAdmin);

        var creatorUserId = await GetCreatorUserIdAsync(courseId, cancellationToken);
        if (creatorUserId is null)
            return null;

        EnsureOwnership(creatorUserId.Value, actingUserId, isAdmin);

        return await BuildTeamAsync(courseId, creatorUserId.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<CourseInstructorDto>?> AssignAsync(int courseId, string email, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CourseTeachingTeamService::AssignAsync: called with CourseId={CourseId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            courseId, actingUserId, isAdmin);

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        var creatorUserId = await GetCreatorUserIdAsync(courseId, cancellationToken);
        if (creatorUserId is null)
            return null;

        EnsureOwnership(creatorUserId.Value, actingUserId, isAdmin);

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user is null)
            throw new InvalidOperationException("No user with that email exists.");

        // Students answering questions would defeat the point of the routing rules, so assignment is
        // limited to the roles the CourseCreatorOrAdmin policy already recognises as teaching staff.
        if (user.Role is not (UserRole.Instructor or UserRole.CourseCreator or UserRole.Admin))
            throw new InvalidOperationException("Only instructors, course creators and admins can be assigned to a course.");

        if (user.Id == creatorUserId.Value)
            throw new InvalidOperationException("The course creator is already part of the teaching team.");

        if (await _dbContext.CourseInstructors.AnyAsync(i => i.CourseId == courseId && i.UserId == user.Id, cancellationToken))
            throw new InvalidOperationException("This user is already assigned to the course.");

        _dbContext.CourseInstructors.Add(
            CourseInstructor.Create(courseId, user.Id, actingUserId, _timeProvider.GetUtcNow().UtcDateTime));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildTeamAsync(courseId, creatorUserId.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<CourseInstructorDto>?> RemoveAsync(int courseId, int userId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CourseTeachingTeamService::RemoveAsync: called with CourseId={CourseId} UserId={UserId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            courseId, userId, actingUserId, isAdmin);

        var creatorUserId = await GetCreatorUserIdAsync(courseId, cancellationToken);
        if (creatorUserId is null)
            return null;

        EnsureOwnership(creatorUserId.Value, actingUserId, isAdmin);

        var assignment = await _dbContext.CourseInstructors
            .FirstOrDefaultAsync(i => i.CourseId == courseId && i.UserId == userId, cancellationToken);

        if (assignment is null)
            return null;

        // Existing threads keep their messages; the removed instructor simply loses access to them.
        _dbContext.CourseInstructors.Remove(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildTeamAsync(courseId, creatorUserId.Value, cancellationToken);
    }

    private async Task<int?> GetCreatorUserIdAsync(int courseId, CancellationToken cancellationToken)
    {
        var creatorUserId = await _dbContext.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => c.CreatedByUserId)
            .FirstOrDefaultAsync(cancellationToken);

        return creatorUserId == 0 ? null : creatorUserId;
    }

    // Mirrors CourseService.EnsureOwnership: holding the Instructor role is not enough, it has to be
    // this course. Assigned instructors deliberately cannot grow the team themselves.
    private static void EnsureOwnership(int creatorUserId, int actingUserId, bool isAdmin)
    {
        if (!isAdmin && creatorUserId != actingUserId)
            throw new QuestionAuthorizationException("You do not have access to this course's teaching team.");
    }

    private async Task<IReadOnlyList<CourseInstructorDto>> BuildTeamAsync(int courseId, int creatorUserId, CancellationToken cancellationToken)
    {
        var assignments = await _dbContext.CourseInstructors
            .AsNoTracking()
            .Where(i => i.CourseId == courseId)
            .Join(_dbContext.Users, i => i.UserId, u => u.Id, (i, u) => new CourseInstructorDto
            {
                UserId = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role,
                AssignedAt = i.AssignedAt,
                IsCreator = false,
            })
            .OrderBy(i => i.AssignedAt)
            .ToListAsync(cancellationToken);

        var creator = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == creatorUserId)
            .Select(u => new CourseInstructorDto
            {
                UserId = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role,
                AssignedAt = u.CreatedAt,
                IsCreator = true,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return creator is null ? assignments : [creator, .. assignments];
    }
}
