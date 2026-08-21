using FluentValidation;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomain.Models.Storage.Enums;

namespace LF.WebApi.Endpoints;

public sealed record CategoryResponse(int Id, string Name, bool IsDefault);

public sealed record QuizOptionResponse(int Id, string Text, bool? IsCorrect, int SortOrder);

public sealed record QuizQuestionResponse(int Id, string Text, string QuestionType, int SortOrder, IReadOnlyList<QuizOptionResponse> Options);

public sealed record LessonPartResponse(
    int Id,
    string PartType,
    int SortOrder,
    string? Html,
    int? StorageObjectId,
    string? MediaUrl,
    IReadOnlyList<QuizQuestionResponse>? QuizQuestions = null,
    int? QuizPassThresholdPercent = null);

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

public sealed record QuizOptionRequest(string Text, bool IsCorrect, int SortOrder);

public sealed record QuizQuestionRequest(string Text, string QuestionType, int SortOrder, IReadOnlyList<QuizOptionRequest> Options);

public sealed record LessonPartRequest(
    string PartType,
    string? Html,
    int? StorageObjectId,
    IReadOnlyList<QuizQuestionRequest>? QuizQuestions = null,
    int? QuizPassThresholdPercent = null);

public sealed record ReplaceLessonPartsRequest(IReadOnlyList<LessonPartRequest> Parts);

public sealed class ReplaceLessonPartsRequestValidator : AbstractValidator<ReplaceLessonPartsRequest>
{
    public ReplaceLessonPartsRequestValidator()
    {
        RuleForEach(x => x.Parts).ChildRules(part =>
        {
            part.RuleFor(p => p.PartType).Must(t => Enum.TryParse<LessonPartType>(t, ignoreCase: true, out _))
                .WithMessage("Part type must be one of Text, Image, Video, Audio, Quiz.");

            part.RuleFor(p => p.Html).NotEmpty()
                .When(p => IsPartType(p.PartType, LessonPartType.Text))
                .WithMessage("Text parts require non-empty content.");

            part.RuleFor(p => p.StorageObjectId).GreaterThan(0)
                .When(p => !IsPartType(p.PartType, LessonPartType.Text) && !IsPartType(p.PartType, LessonPartType.Quiz))
                .WithMessage("Media parts require a storage object id.");

            part.RuleFor(p => p.QuizQuestions).NotEmpty()
                .When(p => IsPartType(p.PartType, LessonPartType.Quiz))
                .WithMessage("Quiz parts require at least one question.");

            part.RuleForEach(p => p.QuizQuestions).ChildRules(question =>
            {
                question.RuleFor(q => q.Text).NotEmpty().WithMessage("Quiz question text cannot be empty.");

                question.RuleFor(q => q.QuestionType).Must(t => Enum.TryParse<QuestionType>(t, ignoreCase: true, out _))
                    .WithMessage("Question type must be SingleChoice or MultipleChoice.");

                question.RuleFor(q => q.Options).Must(o => o.Count >= 2)
                    .WithMessage("Each quiz question requires at least two options.");

                question.RuleFor(q => q).Must(HaveValidCorrectOptionCount)
                    .WithMessage("Single-choice questions require exactly one correct option; multiple-choice questions require at least one.");
            }).When(p => IsPartType(p.PartType, LessonPartType.Quiz));

            part.RuleFor(p => p.QuizPassThresholdPercent).InclusiveBetween(1, 100)
                .When(p => IsPartType(p.PartType, LessonPartType.Quiz) && p.QuizPassThresholdPercent is not null)
                .WithMessage("Quiz pass threshold must be between 1 and 100.");
        });
    }

    private static bool IsPartType(string partType, LessonPartType expected) =>
        Enum.TryParse<LessonPartType>(partType, ignoreCase: true, out var parsed) && parsed == expected;

    private static bool HaveValidCorrectOptionCount(QuizQuestionRequest question)
    {
        if (!Enum.TryParse<QuestionType>(question.QuestionType, ignoreCase: true, out var questionType))
            return true; // caught separately by the QuestionType rule above

        var correctCount = question.Options.Count(o => o.IsCorrect);
        return questionType == QuestionType.SingleChoice ? correctCount == 1 : correctCount >= 1;
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
