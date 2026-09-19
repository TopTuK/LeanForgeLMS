using FluentValidation;
using LF.AppDomain.Entities.Qna;
using LF.AppDomain.Models.Qna.Enums;
using LF.Application.ModelDto.Qna;

namespace LF.WebApi.Endpoints;

public sealed record AskQuestionRequest(int LessonId, string Title, string Body);

public sealed class AskQuestionRequestValidator : AbstractValidator<AskQuestionRequest>
{
    public AskQuestionRequestValidator()
    {
        RuleFor(x => x.LessonId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(LessonQuestion.MaxTitleLength);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(LessonQuestionMessage.MaxBodyLength);
    }
}

public sealed record PostQuestionMessageRequest(string Body);

public sealed class PostQuestionMessageRequestValidator : AbstractValidator<PostQuestionMessageRequest>
{
    public PostQuestionMessageRequestValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(LessonQuestionMessage.MaxBodyLength);
    }
}

public static class LessonQuestionSearch
{
    public const int MaxLength = 200;
}

public sealed record LessonQuestionMessageResponse(
    int Id,
    int AuthorUserId,
    string AuthorName,
    string AuthorRole,
    string? Body,
    DateTime CreatedAt,
    bool IsDeleted,
    bool IsMine);

public sealed record LessonQuestionSummaryResponse(
    int Id,
    int CourseId,
    string CourseTitle,
    int LessonId,
    string LessonTitle,
    int StudentUserId,
    string StudentName,
    string Title,
    string Status,
    DateTime CreatedAt,
    DateTime LastMessageAt,
    int MessageCount,
    bool HasUnread,
    string? LastMessagePreview,
    string LastMessageAuthorRole,
    int? StudentEnrollmentId,
    bool AskedByViewer);

public sealed record LessonQuestionThreadResponse(
    int Id,
    int CourseId,
    string CourseTitle,
    int LessonId,
    string LessonTitle,
    int StudentUserId,
    string StudentName,
    string Title,
    string Status,
    DateTime CreatedAt,
    DateTime LastMessageAt,
    int MessageCount,
    bool HasUnread,
    string? LastMessagePreview,
    string LastMessageAuthorRole,
    int? StudentEnrollmentId,
    bool AskedByViewer,
    IReadOnlyList<LessonQuestionMessageResponse> Messages);

public sealed record PagedLessonQuestionsResponse(
    IReadOnlyList<LessonQuestionSummaryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record QuestionUnreadCountResponse(int Count);

public sealed record LessonQuestionCourseCountResponse(int CourseId, string CourseTitle, int Count);

public sealed record LessonQuestionOverviewResponse(
    int Total,
    int Open,
    int Answered,
    int Closed,
    int Unread,
    IReadOnlyList<LessonQuestionCourseCountResponse> Courses);

// Shared by the student/staff group and the admin oversight group so both surfaces serialise a
// thread identically.
public static class LessonQuestionResponseMapper
{
    public static LessonQuestionSummaryResponse ToResponse(LessonQuestionSummaryDto dto) =>
        new(dto.Id, dto.CourseId, dto.CourseTitle, dto.LessonId, dto.LessonTitle, dto.StudentUserId,
            dto.StudentName, dto.Title, dto.Status.ToString(), dto.CreatedAt, dto.LastMessageAt,
            dto.MessageCount, dto.HasUnread, dto.LastMessagePreview, dto.LastMessageAuthorRole.ToString(),
            dto.StudentEnrollmentId, dto.AskedByViewer);

    public static LessonQuestionThreadResponse ToResponse(LessonQuestionThreadDto dto) =>
        new(dto.Id, dto.CourseId, dto.CourseTitle, dto.LessonId, dto.LessonTitle, dto.StudentUserId,
            dto.StudentName, dto.Title, dto.Status.ToString(), dto.CreatedAt, dto.LastMessageAt,
            dto.MessageCount, dto.HasUnread, dto.LastMessagePreview, dto.LastMessageAuthorRole.ToString(),
            dto.StudentEnrollmentId, dto.AskedByViewer, [.. dto.Messages.Select(ToResponse)]);

    public static LessonQuestionOverviewResponse ToResponse(LessonQuestionOverviewDto dto) =>
        new(dto.Total, dto.Open, dto.Answered, dto.Closed, dto.Unread,
            [.. dto.Courses.Select(c => new LessonQuestionCourseCountResponse(c.CourseId, c.CourseTitle, c.Count))]);

    public static LessonQuestionMessageResponse ToResponse(LessonQuestionMessageDto dto) =>
        new(dto.Id, dto.AuthorUserId, dto.AuthorName, dto.AuthorRole.ToString(), dto.Body, dto.CreatedAt, dto.IsDeleted, dto.IsMine);

    // Enum.TryParse also accepts arbitrary numbers ("7"), so the parsed value is checked too.
    public static bool TryParseStatus(string? status, out LessonQuestionStatus parsed)
    {
        parsed = default;
        return !string.IsNullOrWhiteSpace(status)
            && Enum.TryParse(status, ignoreCase: true, out parsed)
            && Enum.IsDefined(parsed);
    }

    public static LessonQuestionScope ParseScope(string? scope) =>
        string.Equals(scope, "staff", StringComparison.OrdinalIgnoreCase)
            ? LessonQuestionScope.AsStaff
            : LessonQuestionScope.AsStudent;
}
