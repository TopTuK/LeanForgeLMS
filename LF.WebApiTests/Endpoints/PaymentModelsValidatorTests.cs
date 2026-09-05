using LF.WebApi.Endpoints;

namespace LF.WebApiTests.Endpoints;

public class PaymentModelsValidatorTests
{
    private static readonly CheckoutRequestValidator Validator = new();

    [Fact]
    public void Valid_Passes() => Assert.True(Validator.Validate(new CheckoutRequest(5, "SAVE10")).IsValid);

    [Fact]
    public void NoPromoCode_Passes() => Assert.True(Validator.Validate(new CheckoutRequest(5)).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveCourseId_Fails(int courseId) =>
        Assert.False(Validator.Validate(new CheckoutRequest(courseId)).IsValid);

    [Fact]
    public void OverlongPromoCode_Fails() =>
        Assert.False(Validator.Validate(new CheckoutRequest(5, new string('x', 65))).IsValid);
}
