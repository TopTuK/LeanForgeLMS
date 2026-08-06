using System.Security.Claims;
using LF.Application.ModelDto.User;
using LF.Application.Services.Profile;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

public sealed class ProfileEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile").WithTags("Profile").RequireAuthorization();

        group.MapGet("/", async Task<Results<Ok<ProfileResponse>, UnauthorizedHttpResult, NotFound>>
            (ClaimsPrincipal user, IProfileService profileService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var profile = await profileService.GetProfileAsync(userId.Value);
            return profile is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(new ProfileResponse(profile.FirstName, profile.LastName, profile.Email));
        });

        group.MapPut("/", async Task<Results<Ok<ProfileResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem>>
            (UpdateProfileRequest request, ClaimsPrincipal user, IProfileService profileService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new UpdateProfileRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            var dto = new UpdateUserNameDto { FirstName = request.FirstName, LastName = request.LastName };
            var updated = await profileService.UpdateProfileAsync(userId.Value, dto);
            return updated is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(new ProfileResponse(updated.FirstName, updated.LastName, updated.Email));
        });
    }
}
