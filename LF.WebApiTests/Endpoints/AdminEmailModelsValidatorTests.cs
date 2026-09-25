using LF.WebApi.Endpoints;

namespace LF.WebApiTests.Endpoints;

public class AdminEmailModelsValidatorTests
{
    private static readonly SendTestEmailRequestValidator Validator = new();

    [Fact]
    public void Valid_Passes() => Assert.True(Validator.Validate(new SendTestEmailRequest("admin@example.com")).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not-an-email")]
    public void InvalidAddress_Fails(string? to) =>
        Assert.False(Validator.Validate(new SendTestEmailRequest(to!)).IsValid);

    [Fact]
    public void OverlongAddress_Fails() =>
        Assert.False(Validator.Validate(new SendTestEmailRequest(new string('a', 320) + "@example.com")).IsValid);
}
