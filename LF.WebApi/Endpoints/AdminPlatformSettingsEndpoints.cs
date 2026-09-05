using System.Security.Claims;
using LF.Application.ModelDto.Platform;
using LF.Application.Services.Platform;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

public sealed class AdminPlatformSettingsEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/platform-settings").WithTags("AdminPlatformSettings").RequireAuthorization("AdminOnly");

        group.MapGet("/", async Task<Results<Ok<PlatformSettingsResponse>, UnauthorizedHttpResult>>
            (ClaimsPrincipal user, IPlatformSettingsService platformSettings, CancellationToken ct) =>
        {
            if (user.GetUserId() is null) return TypedResults.Unauthorized();

            var settings = await platformSettings.GetAsync(ct);
            return TypedResults.Ok(ToResponse(settings));
        });

        group.MapPut("/student-enrollment", async Task<Results<Ok<PlatformSettingsResponse>, UnauthorizedHttpResult>>
            (UpdateStudentEnrollmentRequest request, ClaimsPrincipal user, IPlatformSettingsService platformSettings, CancellationToken ct) =>
        {
            var adminId = user.GetUserId();
            if (adminId is null) return TypedResults.Unauthorized();

            var settings = await platformSettings.SetStudentEnrollmentEnabledAsync(request.Enabled, adminId.Value, ct);
            return TypedResults.Ok(ToResponse(settings));
        });
    }

    private static PlatformSettingsResponse ToResponse(PlatformSettingsDto dto) =>
        new(dto.StudentEnrollmentEnabled, dto.UpdatedAt, dto.UpdatedByUserId);
}
