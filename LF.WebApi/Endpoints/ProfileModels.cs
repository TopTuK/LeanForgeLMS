using FluentValidation;
using LF.AppDomain.Models.User;

namespace LF.WebApi.Endpoints;

// PreferredLanguage is null until the user picks one; the SPA then uploads its current locale.
public sealed record ProfileResponse(string FirstName, string LastName, string Email, string AvatarUrl, string Role, string? Description, string? PreferredLanguage);

public sealed record UpdateProfileRequest(string FirstName, string? LastName, string? Description);

public sealed record UpdateLanguageRequest(string Language);

public sealed class UpdateLanguageRequestValidator : AbstractValidator<UpdateLanguageRequest>
{
    public UpdateLanguageRequestValidator()
    {
        RuleFor(x => x.Language)
            .NotEmpty()
            .Must(language => UserLanguage.Normalize(language) is not null)
            .WithMessage($"Language must be one of: {string.Join(", ", UserLanguage.Supported)}.");
    }
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
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
