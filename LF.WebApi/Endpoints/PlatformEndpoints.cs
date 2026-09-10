using System.Security.Claims;
using LF.Application.Common;
using LF.Application.Common.Interfaces;
using LF.WebApi.Common;
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
            (ClaimsPrincipal user, IFeatureFlagService featureFlags, CancellationToken ct) =>
        {
            // Same flag and same evaluation context the enrollment guard uses, so the CTA the SPA
            // renders always matches what POST /api/enrollments would actually do.
            var enabled = await featureFlags.IsEnabledAsync(FeatureFlags.SelfEnrollment, user.GetUserId(), ct);
            return TypedResults.Ok(new PlatformConfigResponse(enabled));
        }).RequireAuthorization();
    }
}
