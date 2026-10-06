using LF.Application.ModelDto.Lectures;

namespace LF.Application.Services.Lectures;

// Scheduled online lectures. Staff of a course schedule them for one or more of the course's groups;
// students see the lectures of the groups they belong to.
public interface ILectureService
{
    Task<IReadOnlyList<LectureDto>?> ListForCourseAsync(int courseId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<LectureDto?> ScheduleAsync(int courseId, LectureInputDto input, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<LectureDto?> UpdateAsync(int lectureId, LectureInputDto input, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<LectureDto?> CancelAsync(int lectureId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int lectureId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    // Lectures the user attends (via group membership) or teaches, starting within [from, to).
    Task<IReadOnlyList<LectureDto>> ListMyScheduleAsync(int actingUserId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
