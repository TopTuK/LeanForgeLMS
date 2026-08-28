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

public sealed record QuizAnswerRequest(int QuestionId, IReadOnlyList<int> SelectedOptionIds);

public sealed record SubmitQuizAttemptRequest(IReadOnlyList<QuizAnswerRequest> Answers);

public sealed class SubmitQuizAttemptRequestValidator : AbstractValidator<SubmitQuizAttemptRequest>
{
    public SubmitQuizAttemptRequestValidator()
    {
        RuleFor(x => x.Answers).NotEmpty().WithMessage("At least one answer is required.");

        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.QuestionId).GreaterThan(0);
            answer.RuleFor(a => a.SelectedOptionIds).NotEmpty().WithMessage("Each answer requires at least one selected option.");
        });
    }
}

public sealed record QuizQuestionResultResponse(int QuestionId, bool IsCorrect, IReadOnlyList<int> CorrectOptionIds);

public sealed record QuizAttemptResultResponse(int ScorePercent, bool Passed, IReadOnlyList<QuizQuestionResultResponse> Questions);

public sealed record QuizSubmissionResponse(QuizAttemptResultResponse Result, EnrollmentDetailResponse Enrollment);

public sealed record CoursePreviewLessonResponse(
    int Id,
    string Title,
    int SortOrder,
    bool IncludeInPreview,
    string? Content,
    IReadOnlyList<LessonPartResponse> Parts);

public sealed record CoursePreviewChapterResponse(int Id, string Title, int SortOrder, IReadOnlyList<CoursePreviewLessonResponse> Lessons);

public sealed record CoursePreviewResponse(
    int Id,
    string Title,
    string ShortIntroduction,
    string Description,
    int CategoryId,
    string CategoryName,
    int LessonCount,
    string CoverType,
    string? CoverColor,
    string? CoverImageUrl,
    bool IsEnrolled,
    int? EnrollmentId,
    IReadOnlyList<CoursePreviewChapterResponse> Chapters);
