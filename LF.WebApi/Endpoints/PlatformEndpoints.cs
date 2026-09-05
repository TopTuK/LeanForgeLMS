using LF.Application.Services.Platform;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

public sealed record PlatformConfigResponse(bool StudentEnrollmentEnabled);

// Read-only runtime config the SPA needs before rendering (e.g. whether to show the enroll CTA).
public sealed class PlatformEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/platform").WithTags("Platform");

        group.MapGet("/config", async Task<Ok<PlatformConfigResponse>>
            (IPlatformSettingsService platformSettings, CancellationToken ct) =>
        {
            var enabled = await platformSettings.IsStudentEnrollmentEnabledAsync(ct);
            return TypedResults.Ok(new PlatformConfigResponse(enabled));
        }).RequireAuthorization();
    }
}
