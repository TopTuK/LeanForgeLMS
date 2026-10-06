using LF.Application.Common.Access;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Teaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Teaching;

internal sealed class TeachingCourseService(
    ILogger<TeachingCourseService> logger,
    IAppDbContext dbContext) : ITeachingCourseService
{
    private readonly ILogger<TeachingCourseService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<TeachingCourseDto>> ListMyTeachingCoursesAsync(int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("TeachingCourseService::ListMyTeachingCoursesAsync: called with ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            actingUserId, isAdmin);

        var courses = _dbContext.Courses.AsNoTracking();

        // Admins already see every course in the existing teaching list; keep that behaviour.
        if (!isAdmin)
        {
            var staffCourseIds = _dbContext.StaffCourseIds(actingUserId);
            courses = courses.Where(c => staffCourseIds.Contains(c.Id));
        }

        return await courses
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new TeachingCourseDto
            {
                Id = c.Id,
                Title = c.Title,
                ShortIntroduction = c.ShortIntroduction,
                CategoryName = c.Category.Name,
                IsPublished = c.IsPublished,
                CoverType = c.CoverType,
                CoverColor = c.CoverColor,
                CanEditContent = isAdmin || c.CreatedByUserId == actingUserId,
            })
            .ToListAsync(cancellationToken);
    }
}
