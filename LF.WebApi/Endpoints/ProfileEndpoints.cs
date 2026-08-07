using System.Security.Claims;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.User;
using LF.Application.Services.Profile;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

public sealed class ProfileEndpoints : IEndpointGroup
{
    private const string AvatarUrl = "/api/profile/avatar";

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
                : TypedResults.Ok(new ProfileResponse(profile.FirstName, profile.LastName, profile.Email, AvatarUrl));
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
                : TypedResults.Ok(new ProfileResponse(updated.FirstName, updated.LastName, updated.Email, AvatarUrl));
        });

        group.MapGet("/avatar", async Task<Results<FileStreamHttpResult, UnauthorizedHttpResult, NotFound>>
            (ClaimsPrincipal user, IProfileService profileService, IFileStorageService fileStorageService,
             IWebHostEnvironment env, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var profile = await profileService.GetProfileAsync(userId.Value);
            if (profile is null) return TypedResults.NotFound();

            if (profile.AvatarKey is not null)
            {
                var download = await fileStorageService.DownloadAsync(profile.AvatarKey, ct);
                if (download is not null)
                    return TypedResults.Stream(download.Content, download.ContentType);
            }

            var defaultAvatarPath = Path.Combine(env.WebRootPath, "images", "default-avatar.svg");
            var defaultAvatarStream = File.OpenRead(defaultAvatarPath);
            return TypedResults.Stream(defaultAvatarStream, "image/svg+xml");
        });

        group.MapPost("/avatar", async Task<Results<Ok<ProfileResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem>>
            (IFormFile file, ClaimsPrincipal user, IProfileService profileService, IFileStorageService fileStorageService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            if (file.Length == 0 || file.Length > AvatarUpload.MaxSizeBytes
                || !AvatarUpload.AllowedContentTypes.TryGetValue(file.ContentType, out var extension))
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["Avatar must be a PNG, JPEG or WEBP image no larger than 5 MB."],
                });
            }

            var profile = await profileService.GetProfileAsync(userId.Value);
            if (profile is null) return TypedResults.NotFound();

            var newAvatarKey = $"avatars/{userId.Value}/{Guid.NewGuid():N}{extension}";

            await using (var stream = file.OpenReadStream())
            {
                await fileStorageService.UploadAsync(newAvatarKey, stream, file.ContentType, ct);
            }

            var updated = await profileService.UpdateAvatarAsync(userId.Value, newAvatarKey);
            if (updated is null)
            {
                await fileStorageService.DeleteAsync(newAvatarKey, ct);
                return TypedResults.NotFound();
            }

            if (profile.AvatarKey is not null)
                await fileStorageService.DeleteAsync(profile.AvatarKey, ct);

            return TypedResults.Ok(new ProfileResponse(updated.FirstName, updated.LastName, updated.Email, AvatarUrl));
        }).DisableAntiforgery();

        group.MapDelete("/avatar", async Task<Results<Ok<ProfileResponse>, UnauthorizedHttpResult, NotFound>>
            (ClaimsPrincipal user, IProfileService profileService, IFileStorageService fileStorageService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var profile = await profileService.GetProfileAsync(userId.Value);
            if (profile is null) return TypedResults.NotFound();

            var updated = await profileService.UpdateAvatarAsync(userId.Value, null);
            if (updated is null) return TypedResults.NotFound();

            if (profile.AvatarKey is not null)
                await fileStorageService.DeleteAsync(profile.AvatarKey, ct);

            return TypedResults.Ok(new ProfileResponse(updated.FirstName, updated.LastName, updated.Email, AvatarUrl));
        });
    }
}
