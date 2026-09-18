using FluentValidation;
using LF.Application.ModelDto.Qna;

namespace LF.WebApi.Endpoints;

public sealed record AssignCourseInstructorRequest(string Email);

public sealed class AssignCourseInstructorRequestValidator : AbstractValidator<AssignCourseInstructorRequest>
{
    public const int MaxEmailLength = 256;

    public AssignCourseInstructorRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(MaxEmailLength).EmailAddress();
    }
}

public sealed record CourseInstructorResponse(
    int UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    DateTime AssignedAt,
    bool IsCreator);

public static class CourseInstructorResponseMapper
{
    public static CourseInstructorResponse ToResponse(CourseInstructorDto dto) =>
        new(dto.UserId, dto.Email, dto.FirstName, dto.LastName, dto.Role.ToString(), dto.AssignedAt, dto.IsCreator);
}
