using LF.AppDomain.Models.User.Enums;
using LF.WebApi.Models.Options;

namespace LF.WebApiTests.Models;

public class DevAuthOptionsTests
{
    [Theory]
    [InlineData(UserRole.Student)]
    [InlineData(UserRole.Instructor)]
    [InlineData(UserRole.CourseCreator)]
    [InlineData(UserRole.Admin)]
    public void GetPersona_SupportedRole_ReturnsMatchingPersona(UserRole role)
    {
        var options = new DevAuthOptions();

        var persona = options.GetPersona(role);

        var expected = role switch
        {
            UserRole.Student => options.Student,
            UserRole.Instructor => options.Instructor,
            UserRole.CourseCreator => options.CourseCreator,
            UserRole.Admin => options.Admin,
            _ => throw new InvalidOperationException(),
        };
        Assert.Same(expected, persona);
        Assert.False(string.IsNullOrWhiteSpace(persona.Email));
    }

    [Fact]
    public void GetPersona_NoneRole_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DevAuthOptions().GetPersona(UserRole.None));
    }

    [Fact]
    public void DefaultAdminPersona_HasLocalEmail()
    {
        Assert.Equal("dev.admin@leanforge.local", new DevAuthOptions().Admin.Email);
    }
}
