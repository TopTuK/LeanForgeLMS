using FluentValidation;

namespace LF.WebApi.Endpoints;

public sealed record ProfileResponse(string FirstName, string LastName, string Email, string AvatarUrl);

public sealed record UpdateProfileRequest(string FirstName, string? LastName);

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
    }
}

public static class AvatarUpload
{
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    public static readonly IReadOnlyDictionary<string, string> AllowedContentTypes = new Dictionary<string, string>
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp",
    };
}
