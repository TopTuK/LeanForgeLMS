using LF.AppDomain.Entities.Course;
using LF.AppDomain.Models.Course.Enums;

namespace LF.AppDomainTests.Entities.CourseAggregate;

public class PromoCodeTests
{
    private static PromoCode CreatePercentage(decimal percent = 10m, DateTime? expiresAt = null, int? maxRedemptions = null) =>
        PromoCode.Create("save10", PromoCodeDiscountType.Percentage, percent, courseId: null, expiresAt, maxRedemptions, createdByUserId: 1, DateTime.UtcNow);

    [Fact]
    public void Create_NormalizesCodeToUpperInvariant()
    {
        var promo = CreatePercentage();

        Assert.Equal("SAVE10", promo.Code);
        Assert.True(promo.IsActive);
        Assert.Equal(0, promo.RedemptionCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Create_PercentageOutOfRange_Throws(int percent)
    {
        Assert.Throws<ArgumentException>(() => CreatePercentage(percent));
    }

    [Fact]
    public void Create_FixedAmountNotPositive_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            PromoCode.Create("X", PromoCodeDiscountType.FixedAmount, 0m, null, null, null, 1, DateTime.UtcNow));
    }

    [Fact]
    public void ApplyTo_Percentage_ReducesPrice()
    {
        var promo = CreatePercentage(25m);

        Assert.Equal(1500m, promo.ApplyTo(2000m));
    }

    [Fact]
    public void ApplyTo_FixedAmount_IsClampedToPrice()
    {
        var promo = PromoCode.Create("BIG", PromoCodeDiscountType.FixedAmount, 5000m, null, null, null, 1, DateTime.UtcNow);

        Assert.Equal(0m, promo.ApplyTo(2000m));
    }

    [Fact]
    public void IsRedeemable_Expired_ReturnsFalse()
    {
        var promo = CreatePercentage(expiresAt: DateTime.UtcNow.AddDays(-1));

        Assert.False(promo.IsRedeemable(DateTime.UtcNow));
    }

    [Fact]
    public void IsRedeemable_MaxRedemptionsReached_ReturnsFalse()
    {
        var promo = CreatePercentage(maxRedemptions: 1);
        promo.Redeem(DateTime.UtcNow);

        Assert.False(promo.IsRedeemable(DateTime.UtcNow));
    }

    [Fact]
    public void Redeem_NotRedeemable_Throws()
    {
        var promo = CreatePercentage();
        promo.Deactivate();

        Assert.Throws<InvalidOperationException>(() => promo.Redeem(DateTime.UtcNow));
    }

    [Fact]
    public void AppliesToCourse_GlobalOrMatching()
    {
        var global = CreatePercentage();
        var scoped = PromoCode.Create("C1", PromoCodeDiscountType.Percentage, 10m, courseId: 7, null, null, 1, DateTime.UtcNow);

        Assert.True(global.AppliesToCourse(99));
        Assert.True(scoped.AppliesToCourse(7));
        Assert.False(scoped.AppliesToCourse(8));
    }
}
