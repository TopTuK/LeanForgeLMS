namespace LF.Application.ModelDto.Platform;

public sealed class PlatformSettingsDto
{
    public bool StudentEnrollmentEnabled { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int? UpdatedByUserId { get; init; }
}
