namespace LF.Application.ModelDto.Course;

public sealed class UnpublishCourseResultDto
{
    // Students who keep their enrollment but lose access until the course is published again.
    public int AffectedEnrollmentCount { get; init; }
}
