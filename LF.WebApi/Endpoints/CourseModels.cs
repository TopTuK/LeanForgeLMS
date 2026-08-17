using FluentValidation;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomain.Models.Storage.Enums;

namespace LF.WebApi.Endpoints;

public sealed record CategoryResponse(int Id, string Name, bool IsDefault);

public sealed record LessonPartResponse(int Id, string PartType, int SortOrder, string? Html, int? StorageObjectId, string? MediaUrl);

public sealed record LessonResponse(int Id, string Title, string Content, bool IncludeInPreview, int SortOrder, IReadOnlyList<LessonPartResponse> Parts);

public sealed record ChapterResponse(int Id, string Title, int SortOrder, IReadOnlyList<LessonResponse> Lessons);

public sealed record CourseDetailResponse(
    int Id,
    string Title,
    string ShortIntroduction,
    string Description,
    string CoverType,
    string? CoverColor,
    string? CoverImageUrl,
    bool IsPublished,
    int CategoryId,
    string CategoryName,
    int CreatedByUserId,
    DateTime CreatedAt,
    IReadOnlyList<ChapterResponse> Chapters);

public sealed record CourseSummaryResponse(
    int Id,
    string Title,
    string ShortIntroduction,
    string CoverType,
    string? CoverColor,
    string? CoverImageUrl,
    bool IsPublished,
    int CategoryId,
    string CategoryName,
    int CreatedByUserId,
    DateTime CreatedAt,
    int ChapterCount);

public sealed record PagedCoursesResponse(IReadOnlyList<CourseSummaryResponse> Items, int TotalCount, int Page, int PageSize);

public sealed record CreateCourseRequest(
    string Title,
    string ShortIntroduction,
    string Description,
    int CategoryId,
    string CoverType,
    string? CoverColor,
    int? CoverImageStorageObjectId);

public sealed class CreateCourseRequestValidator : AbstractValidator<CreateCourseRequest>
{
    public CreateCourseRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShortIntroduction).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.CategoryId).GreaterThan(0);

        RuleFor(x => x.CoverType).Must(t => Enum.TryParse<CourseCoverType>(t, out _))
            .WithMessage("Cover type must be one of None, Color, Image.");

        RuleFor(x => x.CoverColor).Must(c => Enum.TryParse<CourseCoverColor>(c, out _))
            .When(x => string.Equals(x.CoverType, nameof(CourseCoverType.Color), StringComparison.OrdinalIgnoreCase))
            .WithMessage("A valid cover color is required when cover type is Color.");

        RuleFor(x => x.CoverImageStorageObjectId).GreaterThan(0)
            .When(x => string.Equals(x.CoverType, nameof(CourseCoverType.Image), StringComparison.OrdinalIgnoreCase))
            .WithMessage("A cover image storage object id is required when cover type is Image.");
    }
}

public sealed record UploadCoverImageResponse(int StorageObjectId);

public static class CourseCoverImageUpload
{
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    public static readonly IReadOnlyDictionary<string, string> AllowedContentTypes = new Dictionary<string, string>
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp",
    };
}

public sealed record AddChapterRequest(string Title);

public sealed class AddChapterRequestValidator : AbstractValidator<AddChapterRequest>
{
    public AddChapterRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public sealed record RenameChapterRequest(string Title);

public sealed class RenameChapterRequestValidator : AbstractValidator<RenameChapterRequest>
{
    public RenameChapterRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public sealed record MoveRequest(string Direction);

public sealed class MoveRequestValidator : AbstractValidator<MoveRequest>
{
    public MoveRequestValidator()
    {
        RuleFor(x => x.Direction).Must(d => d is "Up" or "Down")
            .WithMessage("Direction must be 'Up' or 'Down'.");
    }
}

public sealed record AddLessonRequest(string Title, string? Content, bool IncludeInPreview);

public sealed class AddLessonRequestValidator : AbstractValidator<AddLessonRequest>
{
    public AddLessonRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public sealed record UpdateLessonRequest(string Title, string? Content, bool IncludeInPreview);

public sealed class UpdateLessonRequestValidator : AbstractValidator<UpdateLessonRequest>
{
    public UpdateLessonRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public sealed record LessonPartRequest(string PartType, string? Html, int? StorageObjectId);

public sealed record ReplaceLessonPartsRequest(IReadOnlyList<LessonPartRequest> Parts);

public sealed class ReplaceLessonPartsRequestValidator : AbstractValidator<ReplaceLessonPartsRequest>
{
    public ReplaceLessonPartsRequestValidator()
    {
        RuleForEach(x => x.Parts).ChildRules(part =>
        {
            part.RuleFor(p => p.PartType).Must(t => Enum.TryParse<LessonPartType>(t, ignoreCase: true, out _))
                .WithMessage("Part type must be one of Text, Image, Video, Audio.");

            part.RuleFor(p => p.Html).NotEmpty()
                .When(p => string.Equals(p.PartType, nameof(LessonPartType.Text), StringComparison.OrdinalIgnoreCase))
                .WithMessage("Text parts require non-empty content.");

            part.RuleFor(p => p.StorageObjectId).GreaterThan(0)
                .When(p => !string.Equals(p.PartType, nameof(LessonPartType.Text), StringComparison.OrdinalIgnoreCase))
                .WithMessage("Media parts require a storage object id.");
        });
    }
}

public static class LessonMediaUpload
{
    public static readonly IReadOnlyDictionary<string, (StorageObjectType ObjectType, long MaxSizeBytes)> AllowedContentTypes =
        new Dictionary<string, (StorageObjectType, long)>
        {
            ["image/png"] = (StorageObjectType.Image, 5 * 1024 * 1024),
            ["image/jpeg"] = (StorageObjectType.Image, 5 * 1024 * 1024),
            ["image/webp"] = (StorageObjectType.Image, 5 * 1024 * 1024),
            ["image/gif"] = (StorageObjectType.Image, 5 * 1024 * 1024),
            ["video/mp4"] = (StorageObjectType.Video, 200 * 1024 * 1024),
            ["video/webm"] = (StorageObjectType.Video, 200 * 1024 * 1024),
            ["audio/mpeg"] = (StorageObjectType.Audio, 50 * 1024 * 1024),
            ["audio/wav"] = (StorageObjectType.Audio, 50 * 1024 * 1024),
            ["audio/ogg"] = (StorageObjectType.Audio, 50 * 1024 * 1024),
            ["audio/webm"] = (StorageObjectType.Audio, 50 * 1024 * 1024),
        };
}
