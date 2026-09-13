using FluentValidation;
using LF.AppDomain.Entities.News;
using LF.AppDomain.Models.News.Enums;

namespace LF.WebApi.Endpoints;

public sealed record SaveNewsPostRequest(
    string Title,
    string Html,
    string Visibility,
    bool IsPublished,
    IReadOnlyList<int>? ImageStorageObjectIds);

public sealed class SaveNewsPostRequestValidator : AbstractValidator<SaveNewsPostRequest>
{
    public const int MaxHtmlLength = 50_000;

    public SaveNewsPostRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(NewsPost.MaxTitleLength);
        RuleFor(x => x.Html).NotEmpty().MaximumLength(MaxHtmlLength);

        RuleFor(x => x.Visibility).Must(BeKnownVisibility)
            .WithMessage("Visibility must be Public or MembersOnly.");

        RuleFor(x => x.ImageStorageObjectIds).Must(ids => ids is null || ids.Count <= NewsPost.MaxImages)
            .WithMessage($"A news post can have at most {NewsPost.MaxImages} images.");

        RuleFor(x => x.ImageStorageObjectIds).Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Each image can only be attached once.");

        RuleForEach(x => x.ImageStorageObjectIds).GreaterThan(0);
    }

    // Enum.TryParse also accepts arbitrary numbers ("7"), so the parsed value is checked too.
    internal static bool BeKnownVisibility(string? visibility) =>
        Enum.TryParse<NewsVisibility>(visibility, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed);
}

public sealed record AdminNewsImageResponse(int Id, int StorageObjectId, string Url);

public sealed record AdminNewsPostResponse(
    int Id,
    string Title,
    string Html,
    string Visibility,
    bool IsPublished,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<AdminNewsImageResponse> Images);

public sealed record PagedAdminNewsResponse(IReadOnlyList<AdminNewsPostResponse> Items, int TotalCount, int Page, int PageSize);

public sealed record UploadNewsImageResponse(int StorageObjectId);

public static class NewsImageUpload
{
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>
    {
        "image/png",
        "image/jpeg",
        "image/webp",
    };
}
