using FluentValidation;
using LF.AppDomain.Entities.Groups;
using LF.Application.ModelDto.Groups;

namespace LF.WebApi.Endpoints;

public sealed record StudentGroupRequest(string Name, string? Description);

public sealed class StudentGroupRequestValidator : AbstractValidator<StudentGroupRequest>
{
    public StudentGroupRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(StudentGroup.MaxNameLength);
        RuleFor(x => x.Description).MaximumLength(StudentGroup.MaxDescriptionLength);
    }
}

public sealed record AddGroupMembersRequest(IReadOnlyList<int> UserIds);

public sealed class AddGroupMembersRequestValidator : AbstractValidator<AddGroupMembersRequest>
{
    public const int MaxUsersPerRequest = 500;

    public AddGroupMembersRequestValidator()
    {
        RuleFor(x => x.UserIds).NotEmpty().Must(ids => ids.Count <= MaxUsersPerRequest)
            .WithMessage($"At most {MaxUsersPerRequest} students can be added at once.");
        RuleForEach(x => x.UserIds).GreaterThan(0);
    }
}

public sealed record StudentGroupSummaryResponse(
    int Id,
    int CourseId,
    string CourseTitle,
    string Name,
    string? Description,
    int MemberCount,
    DateTime CreatedAt,
    DateTime? NextLectureStartsAt,
    bool IsTeaching);

public sealed record StudentGroupMemberResponse(
    int UserId,
    string FirstName,
    string LastName,
    string? Email,
    DateTime AddedAt,
    bool IsEnrolled);

public sealed record StudentGroupDetailResponse(
    int Id,
    int CourseId,
    string CourseTitle,
    string Name,
    string? Description,
    DateTime CreatedAt,
    bool CanManage,
    IReadOnlyList<StudentGroupMemberResponse> Members);

public sealed record EligibleStudentResponse(int UserId, string FirstName, string LastName, string Email);

public static class StudentGroupResponseMapper
{
    public static StudentGroupSummaryResponse ToResponse(StudentGroupSummaryDto dto) =>
        new(dto.Id, dto.CourseId, dto.CourseTitle, dto.Name, dto.Description, dto.MemberCount, dto.CreatedAt, dto.NextLectureStartsAt, dto.IsTeaching);

    public static StudentGroupDetailResponse ToResponse(StudentGroupDetailDto dto) =>
        new(dto.Id, dto.CourseId, dto.CourseTitle, dto.Name, dto.Description, dto.CreatedAt, dto.CanManage,
            [.. dto.Members.Select(m => new StudentGroupMemberResponse(m.UserId, m.FirstName, m.LastName, m.Email, m.AddedAt, m.IsEnrolled))]);

    public static EligibleStudentResponse ToResponse(EligibleStudentDto dto) =>
        new(dto.UserId, dto.FirstName, dto.LastName, dto.Email);
}
