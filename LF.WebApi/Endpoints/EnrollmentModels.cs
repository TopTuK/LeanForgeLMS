using FluentValidation;

namespace LF.WebApi.Endpoints;

public sealed record CourseCatalogItemResponse(
    int Id,
    string Title,
    string ShortIntroduction,
    int CategoryId,
    string CategoryName,
    int LessonCount,
    string CoverType,
    string? CoverColor,
    string? CoverImageUrl);

public sealed record PagedCourseCatalogResponse(IReadOnlyList<CourseCatalogItemResponse> Items, int TotalCount, int Page, int PageSize);

public sealed record EnrollRequest(int CourseId);

public sealed class EnrollRequestValidator : AbstractValidator<EnrollRequest>
{
    public EnrollRequestValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0);
    }
}

public sealed record EnrollmentSummaryResponse(
    int Id,
    int CourseId,
    string CourseTitle,
    string CourseShortIntroduction,
    string CategoryName,
    int TotalLessonCount,
    int CompletedLessonCount,
    int ProgressPercent,
    DateTime EnrolledAt,
    DateTime? CompletedAt,
    string CoverType,
    string? CoverColor,
    string? CoverImageUrl);

public sealed record EnrollmentLessonResponse(int Id, string Title, string Content, int SortOrder, bool IsCompleted, IReadOnlyList<LessonPartResponse> Parts);

public sealed record EnrollmentChapterResponse(int Id, string Title, int SortOrder, IReadOnlyList<EnrollmentLessonResponse> Lessons);

public sealed record EnrollmentDetailResponse(
    int Id,
    int CourseId,
    string CourseTitle,
    string CourseDescription,
    DateTime EnrolledAt,
    DateTime? CompletedAt,
    IReadOnlyList<EnrollmentChapterResponse> Chapters);
