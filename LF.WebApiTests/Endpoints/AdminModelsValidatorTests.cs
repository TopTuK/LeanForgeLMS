using LF.WebApi.Endpoints;

namespace LF.WebApiTests.Endpoints;

public class AdminModelsValidatorTests
{
    [Fact]
    public void UpdateAdminUserInfo_ValidRequest_Passes()
    {
        var result = new UpdateAdminUserInfoRequestValidator()
            .Validate(new UpdateAdminUserInfoRequest("Ada", "Lovelace", "Bio."));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void UpdateAdminUserInfo_MissingFirstName_Fails(string? firstName)
    {
        var result = new UpdateAdminUserInfoRequestValidator()
            .Validate(new UpdateAdminUserInfoRequest(firstName!, null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateAdminUserInfo_DescriptionTooLong_Fails()
    {
        var result = new UpdateAdminUserInfoRequestValidator()
            .Validate(new UpdateAdminUserInfoRequest("Ada", null, new string('a', 501)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateCategory_ValidName_Passes()
    {
        var result = new CreateCategoryRequestValidator().Validate(new CreateCategoryRequest("Agile"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CreateCategory_MissingName_Fails(string? name)
    {
        var result = new CreateCategoryRequestValidator().Validate(new CreateCategoryRequest(name!));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateCategory_NameTooLong_Fails()
    {
        var result = new CreateCategoryRequestValidator().Validate(new CreateCategoryRequest(new string('a', 101)));

        Assert.False(result.IsValid);
    }
}
