using LF.WebApi.Endpoints;

namespace LF.WebApiTests.Endpoints;

public class UpdateLanguageRequestValidatorTests
{
    private static readonly UpdateLanguageRequestValidator Validator = new();

    [Theory]
    [InlineData("ru")]
    [InlineData("en")]
    [InlineData("EN")]
    public void SupportedLanguage_Passes(string language) =>
        Assert.True(Validator.Validate(new UpdateLanguageRequest(language)).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("de")]
    [InlineData("russian")]
    public void UnsupportedLanguage_Fails(string? language) =>
        Assert.False(Validator.Validate(new UpdateLanguageRequest(language!)).IsValid);
}
