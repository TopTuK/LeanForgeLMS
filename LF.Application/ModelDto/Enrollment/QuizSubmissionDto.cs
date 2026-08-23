namespace LF.Application.ModelDto.Enrollment;

public sealed class QuizSubmissionDto
{
    public QuizAttemptResultDto Result { get; init; } = null!;
    public EnrollmentDetailDto Enrollment { get; init; } = null!;
}
