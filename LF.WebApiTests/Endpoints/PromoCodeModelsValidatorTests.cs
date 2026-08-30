using LF.AppDomain.Models.Course.Enums;
using LF.WebApi.Endpoints;

namespace LF.WebApiTests.Endpoints;

public class PromoCodeModelsValidatorTests
{
    private static readonly CreatePromoCodeRequestValidator Validator = new();

    private static CreatePromoCodeRequest Valid() => new(
        Code: "SAVE10",
        DiscountType: nameof(PromoCodeDiscountType.Percentage),
        DiscountValue: 10m,
        CourseId: null,
        ExpiresAt: null,
        MaxRedemptions: null);

    [Fact]
    public void Valid_Passes() => Assert.True(Validator.Validate(Valid()).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MissingCode_Fails(string? code) =>
        Assert.False(Validator.Validate(Valid() with { Code = code! }).IsValid);

    [Fact]
    public void UnknownDiscountType_Fails() =>
        Assert.False(Validator.Validate(Valid() with { DiscountType = "BuyOneGetOne" }).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(150)]
    public void PercentageOutOfRange_Fails(int value) =>
        Assert.False(Validator.Validate(Valid() with { DiscountValue = value }).IsValid);

    [Fact]
    public void FixedAmountNotPositive_Fails() =>
        Assert.False(Validator.Validate(Valid() with { DiscountType = nameof(PromoCodeDiscountType.FixedAmount), DiscountValue = 0m }).IsValid);

    [Fact]
    public void FixedAmountPositive_Passes() =>
        Assert.True(Validator.Validate(Valid() with { DiscountType = nameof(PromoCodeDiscountType.FixedAmount), DiscountValue = 500m }).IsValid);

    [Fact]
    public void PastExpiry_Fails() =>
        Assert.False(Validator.Validate(Valid() with { ExpiresAt = DateTime.UtcNow.AddDays(-1) }).IsValid);

    [Fact]
    public void NonPositiveMaxRedemptions_Fails() =>
        Assert.False(Validator.Validate(Valid() with { MaxRedemptions = 0 }).IsValid);
}
