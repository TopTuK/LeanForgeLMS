namespace LF.WebApi.Endpoints;

public sealed record PlatformSettingsResponse(bool StudentEnrollmentEnabled, DateTime UpdatedAt, int? UpdatedByUserId);

public sealed record UpdateStudentEnrollmentRequest(bool Enabled);
