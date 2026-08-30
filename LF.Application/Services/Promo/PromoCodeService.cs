using LF.AppDomain.Entities.Course;
using LF.AppDomain.Models.Course.Enums;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Promo;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Promo;

internal sealed class PromoCodeService(ILogger<PromoCodeService> logger, IAppDbContext dbContext, TimeProvider timeProvider) : IPromoCodeService
{
    private readonly ILogger<PromoCodeService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<PromoCodeDto> CreatePromoCodeAsync(CreatePromoCodeDto dto, int createdByUserId)
    {
        _logger.LogInformation("PromoCodeService::CreatePromoCodeAsync: called with Code={Code} CourseId={CourseId} CreatedByUserId={CreatedByUserId}",
            dto.Code, dto.CourseId, createdByUserId);

        string? courseTitle = null;
        if (dto.CourseId is { } courseId)
        {
            var course = await _dbContext.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
            if (course is null)
                throw new ArgumentException($"Course {courseId} not found.", nameof(dto));

            if (course.PricingType != CoursePricingType.Paid)
                throw new ArgumentException("A course-scoped promo code can only target a paid course.", nameof(dto));

            courseTitle = course.Title;
        }

        var promoCode = PromoCode.Create(dto.Code, dto.DiscountType, dto.DiscountValue, dto.CourseId, dto.ExpiresAt,
            dto.MaxRedemptions, createdByUserId, _timeProvider.GetUtcNow().UtcDateTime);

        var duplicate = await _dbContext.PromoCodes.AnyAsync(p => p.Code == promoCode.Code);
        if (duplicate)
            throw new ArgumentException($"Promo code '{promoCode.Code}' already exists.", nameof(dto));

        _dbContext.PromoCodes.Add(promoCode);
        await _dbContext.SaveChangesAsync();

        var result = promoCode.Adapt<PromoCodeDto>();
        return courseTitle is null ? result : result with { CourseTitle = courseTitle };
    }

    public async Task<PagedPromoCodesDto> ListPromoCodesAsync(int page, int pageSize)
    {
        _logger.LogInformation("PromoCodeService::ListPromoCodesAsync: called with Page={Page} PageSize={PageSize}", page, pageSize);

        var query = _dbContext.PromoCodes.AsNoTracking().Include(p => p.Course);

        var totalCount = await query.CountAsync();
        var promoCodes = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedPromoCodesDto { Items = [.. promoCodes.Select(p => p.Adapt<PromoCodeDto>())], TotalCount = totalCount };
    }

    public async Task<bool> DeactivatePromoCodeAsync(int id)
    {
        _logger.LogInformation("PromoCodeService::DeactivatePromoCodeAsync: called with Id={PromoCodeId}", id);

        var promoCode = await _dbContext.PromoCodes.FirstOrDefaultAsync(p => p.Id == id);
        if (promoCode is null)
            return false;

        promoCode.Deactivate();
        await _dbContext.SaveChangesAsync();

        return true;
    }
}
