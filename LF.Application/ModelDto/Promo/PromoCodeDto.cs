using LF.AppDomain.Models.Course.Enums;

namespace LF.Application.ModelDto.Promo;

public sealed record PromoCodeDto
{
    public int Id { get; init; }
    public string Code { get; init; } = null!;
    public PromoCodeDiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public int? CourseId { get; init; }
    public string? CourseTitle { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public int? MaxRedemptions { get; init; }
    public int RedemptionCount { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
