using LF.AppDomain.Models.Course.Enums;

namespace LF.AppDomain.Entities.Course;

public sealed class PromoCode
{
    public const int MaxCodeLength = 64;

    private PromoCode()
    {
    }

    public int Id { get; private set; }
    public string Code { get; private set; } = null!;
    public PromoCodeDiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public int? CourseId { get; private set; }
    public Course? Course { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public int? MaxRedemptions { get; private set; }
    public int RedemptionCount { get; private set; }
    public bool IsActive { get; private set; }
    public int CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static PromoCode Create(
        string code,
        PromoCodeDiscountType discountType,
        decimal discountValue,
        int? courseId,
        DateTime? expiresAt,
        int? maxRedemptions,
        int createdByUserId,
        DateTime createdAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(createdByUserId, 0);

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Promo code cannot be empty.", nameof(code));

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length > MaxCodeLength)
            throw new ArgumentException($"Promo code cannot exceed {MaxCodeLength} characters.", nameof(code));

        if (!Enum.IsDefined(discountType))
            throw new ArgumentException("Unknown promo code discount type.", nameof(discountType));

        switch (discountType)
        {
            case PromoCodeDiscountType.Percentage when discountValue is < 1 or > 100:
                throw new ArgumentException("A percentage discount must be between 1 and 100.", nameof(discountValue));
            case PromoCodeDiscountType.FixedAmount when discountValue <= 0:
                throw new ArgumentException("A fixed discount must be greater than zero.", nameof(discountValue));
        }

        if (courseId is <= 0)
            throw new ArgumentException("Course id must be positive when provided.", nameof(courseId));

        if (maxRedemptions is <= 0)
            throw new ArgumentException("Max redemptions must be positive when provided.", nameof(maxRedemptions));

        return new PromoCode
        {
            Code = normalized,
            DiscountType = discountType,
            DiscountValue = decimal.Round(discountValue, 2),
            CourseId = courseId,
            ExpiresAt = expiresAt,
            MaxRedemptions = maxRedemptions,
            RedemptionCount = 0,
            IsActive = true,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
        };
    }

    public bool AppliesToCourse(int courseId) => CourseId is null || CourseId == courseId;

    public bool IsRedeemable(DateTime nowUtc) =>
        IsActive
        && (ExpiresAt is null || ExpiresAt > nowUtc)
        && (MaxRedemptions is null || RedemptionCount < MaxRedemptions);

    public decimal DiscountFor(decimal price)
    {
        if (price <= 0)
            return 0m;

        var discount = DiscountType == PromoCodeDiscountType.Percentage
            ? price * DiscountValue / 100m
            : DiscountValue;

        return decimal.Round(Math.Clamp(discount, 0m, price), 2);
    }

    public decimal ApplyTo(decimal price) => decimal.Round(price - DiscountFor(price), 2);

    public void Redeem(DateTime nowUtc)
    {
        if (!IsRedeemable(nowUtc))
            throw new InvalidOperationException("Promo code is not redeemable.");

        RedemptionCount++;
    }

    public void Deactivate() => IsActive = false;
}
