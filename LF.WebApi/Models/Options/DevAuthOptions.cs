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

    public DevAuthPersona CourseCreator { get; set; } = new()
    {
        Email = "dev.creator@leanforge.local",
        FirstName = "Dev",
        LastName = "Creator",
    };
}

public sealed class DevAuthPersona
{
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = string.Empty;
}
