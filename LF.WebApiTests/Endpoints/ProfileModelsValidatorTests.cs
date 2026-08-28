using LF.WebApi.Endpoints;

namespace LF.WebApiTests.Endpoints;

public class ProfileModelsValidatorTests
{
    private static UpdateProfileRequest Valid() => new("Ada", "Lovelace", "Mathematician.");

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var result = new UpdateProfileRequestValidator().Validate(Valid());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_MissingFirstName_Fails(string? firstName)
    {
        var request = Valid() with { FirstName = firstName! };

        var result = new UpdateProfileRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileRequest.FirstName));
    }

    [Fact]
    public void Validate_FirstNameTooLong_Fails()
    {
        var request = Valid() with { FirstName = new string('a', 101) };

        var result = new UpdateProfileRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileRequest.FirstName));
    }

    [Fact]
    public void Validate_LastNameTooLong_Fails()
    {
        var request = Valid() with { LastName = new string('a', 101) };

        var result = new UpdateProfileRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileRequest.LastName));
    }

    [Fact]
    public void Validate_NullLastNameAndDescription_Passes()
    {
        var result = new UpdateProfileRequestValidator().Validate(new UpdateProfileRequest("Ada", null, null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DescriptionTooLong_Fails()
    {
        var request = Valid() with { Description = new string('a', 501) };

        var result = new UpdateProfileRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileRequest.Description));
    }
}
