namespace LF.Infrastructure.Persistence.Seed;

internal sealed class DefaultAdminEntry
{
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
