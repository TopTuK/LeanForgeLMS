using LF.AppDomain.Models.User.Enums;

namespace LF.WebApi.Models.Options;

public sealed class DevAuthOptions
{
    public const string SectionName = "DevAuth";

    public DevAuthPersona Student { get; set; } = new()
    {
        Email = "dev.student@leanforge.local",
        FirstName = "Dev",
        LastName = "Student",
    };

    public DevAuthPersona Instructor { get; set; } = new()
    {
        Email = "dev.instructor@leanforge.local",
        FirstName = "Dev",
        LastName = "Instructor",
    };

    public DevAuthPersona CourseCreator { get; set; } = new()
    {
        Email = "dev.creator@leanforge.local",
        FirstName = "Dev",
        LastName = "Creator",
    };

    public DevAuthPersona GetPersona(UserRole role) => role switch
    {
        UserRole.Student => Student,
        UserRole.Instructor => Instructor,
        UserRole.CourseCreator => CourseCreator,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "No dev persona configured for this role."),
    };
}

public sealed class DevAuthPersona
{
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = string.Empty;
}
