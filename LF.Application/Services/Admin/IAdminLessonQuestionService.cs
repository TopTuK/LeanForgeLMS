using LF.AppDomain.Models.Qna.Enums;
using LF.Application.ModelDto.Qna;

namespace LF.Application.Services.Admin;

// Platform-wide oversight. Access is gated by the "AdminOnly" route policy, so unlike
// ILessonQuestionService these methods take no actingUserId for authorization — only for
// attribution on the rows they write.
public interface IAdminLessonQuestionService
{
    Task<PagedLessonQuestionsDto> ListAsync(
        int? courseId,
        LessonQuestionStatus? status,
        string? search,
        int adminUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<LessonQuestionThreadDto?> GetThreadAsync(int questionId, int adminUserId, CancellationToken cancellationToken = default);

    Task<LessonQuestionThreadDto?> PostMessageAsync(int questionId, PostQuestionMessageDto message, int adminUserId, CancellationToken cancellationToken = default);

    Task<LessonQuestionThreadDto?> DeleteMessageAsync(int questionId, int messageId, int adminUserId, CancellationToken cancellationToken = default);

    Task<bool> DeleteThreadAsync(int questionId, CancellationToken cancellationToken = default);
}
