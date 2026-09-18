using LF.AppDomain.Models.Qna.Enums;
using LF.Application.ModelDto.Qna;

namespace LF.Application.Services.Qna;

// The student and teaching-staff surface. Every method takes actingUserId + isAdmin and enforces
// access itself: a global Instructor role says nothing about any particular course, so this cannot
// be delegated to a route authorization policy.
//
// A null return means "no such thread/lesson" (the caller maps it to 404); an access failure throws
// QuestionAuthorizationException (mapped to 403).
public interface ILessonQuestionService
{
    Task<LessonQuestionThreadDto?> AskAsync(AskQuestionDto question, int studentUserId, CancellationToken cancellationToken = default);

    Task<PagedLessonQuestionsDto> ListAsync(
        int actingUserId,
        LessonQuestionScope scope,
        int? courseId,
        LessonQuestionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedLessonQuestionsDto> ListForLessonAsync(
        int lessonId,
        int actingUserId,
        bool isAdmin,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<LessonQuestionThreadDto?> GetThreadAsync(int questionId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<LessonQuestionThreadDto?> PostMessageAsync(
        int questionId,
        PostQuestionMessageDto message,
        int actingUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<LessonQuestionThreadDto?> SetStatusAsync(int questionId, bool close, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    // Counts threads awaiting the caller's attention across both of their inboxes. Admins get their
    // own two inboxes here, not the whole platform — the oversight section is a separate surface.
    Task<int> GetUnreadCountAsync(int actingUserId, CancellationToken cancellationToken = default);
}
