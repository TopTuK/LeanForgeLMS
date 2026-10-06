using LF.Application.ModelDto.Groups;

namespace LF.Application.Services.Groups;

// Student groups (cohorts) within a course. Management is limited to the course's teaching staff
// (creator + assigned instructors) and admins; members can read their own groups. Every method
// takes actingUserId + isAdmin and returns null when the course/group doesn't exist.
public interface IStudentGroupService
{
    Task<IReadOnlyList<StudentGroupSummaryDto>?> ListForCourseAsync(int courseId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<StudentGroupDetailDto?> GetAsync(int groupId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<StudentGroupDetailDto?> CreateAsync(int courseId, string name, string? description, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<StudentGroupDetailDto?> UpdateAsync(int groupId, string name, string? description, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int groupId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<StudentGroupDetailDto?> AddMembersAsync(int groupId, IReadOnlyCollection<int> userIds, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<StudentGroupDetailDto?> RemoveMemberAsync(int groupId, int userId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EligibleStudentDto>?> ListEligibleStudentsAsync(int courseId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentGroupSummaryDto>> ListMyGroupsAsync(int actingUserId, CancellationToken cancellationToken = default);
}
