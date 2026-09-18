using LF.WebApi.Endpoints;

namespace LF.WebApiTests.Endpoints;

public class CourseInstructorModelsValidatorTests
{
    private static bool IsValid(string email) =>
        new AssignCourseInstructorRequestValidator().Validate(new AssignCourseInstructorRequest(email)).IsValid;

    [Theory]
    [InlineData("instructor@lf.test")]
    [InlineData("first.last+tag@sub.domain.example")]
    // FluentValidation's EmailAddress() deliberately allows a bare host, which is valid on an
    // intranet; the authoritative check is that the address matches a real user row anyway.
    [InlineData("intranet@host")]
    public void Assign_ValidEmail_Passes(string email) => Assert.True(IsValid(email));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("@lf.test")]
    public void Assign_InvalidEmail_Fails(string email) => Assert.False(IsValid(email));

    [Fact]
    public void Assign_EmailTooLong_Fails() =>
        Assert.False(IsValid(new string('a', AssignCourseInstructorRequestValidator.MaxEmailLength) + "@lf.test"));
}
