using FluentValidation;

namespace LF.WebApi.Endpoints;

public sealed record CategoryResponse(int Id, string Name);

public sealed record LessonResponse(int Id, string Title, string Content, bool IncludeInPreview, int SortOrder);

public sealed record ChapterResponse(int Id, string Title, int SortOrder, IReadOnlyList<LessonResponse> Lessons);

public sealed record CourseDetailResponse(
    int Id,
    string Title,
    string ShortIntroduction,
    string Description,
    string? ImageKey,
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
    bool IsPublished,
    int CategoryId,
    string CategoryName,
    int CreatedByUserId,
    DateTime CreatedAt,
    int ChapterCount);

public sealed record PagedCoursesResponse(IReadOnlyList<CourseSummaryResponse> Items, int TotalCount, int Page, int PageSize);

public sealed record CreateCourseRequest(string Title, string ShortIntroduction, string Description, int CategoryId);

public sealed class CreateCourseRequestValidator : AbstractValidator<CreateCourseRequest>
{
    public CreateCourseRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShortIntroduction).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.CategoryId).GreaterThan(0);
    }
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
