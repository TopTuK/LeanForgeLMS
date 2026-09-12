using FluentValidation;

namespace LF.WebApi.Endpoints;

public sealed record EnrollUserRequest(int UserId);

public sealed class EnrollUserRequestValidator : AbstractValidator<EnrollUserRequest>
{
    public EnrollUserRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}

public sealed record AdminCourseSummaryResponse(
    int Id,
    string Title,
    string ShortIntroduction,
    bool IsPublished,
    int CategoryId,
    string CategoryName,
    int CreatedByUserId,
    string? AuthorEmail,
    string? AuthorFirstName,
    string? AuthorLastName,
    DateTime CreatedAt,
    int ChapterCount,
    string PricingType,
    decimal? Price,
    string EnrollmentMode);

public sealed record PagedAdminCoursesResponse(
    IReadOnlyList<AdminCourseSummaryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AdminCourseEnrollmentResponse(
    int Id,
    int UserId,
    string? StudentEmail,
    string? StudentFirstName,
    string? StudentLastName,
    string Status,
    decimal PricePaid,
    bool IsPaid,
    DateTime EnrolledAt,
    DateTime? CompletedAt,
    int TotalLessonCount,
    int CompletedLessonCount,
    int ProgressPercent);

public sealed record PagedAdminCourseEnrollmentsResponse(
    IReadOnlyList<AdminCourseEnrollmentResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record RemoveEnrollmentResponse(int UserId, bool WasPaid, decimal PricePaid);

public sealed record DeleteCourseResponse(int RemovedEnrollmentCount, int PaidEnrollmentCount);
