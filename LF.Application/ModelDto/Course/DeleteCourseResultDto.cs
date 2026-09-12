namespace LF.Application.ModelDto.Course;

public sealed class DeleteCourseResultDto
{
    public int RemovedEnrollmentCount { get; init; }
    public int PaidEnrollmentCount { get; init; }

    // MinIO keys orphaned by the delete. LF.CourseService has no MinIO client, so it reports
    // them and LF.WebApi removes the blobs once the delete has committed.
    public IReadOnlyList<string> StorageObjectKeys { get; init; } = [];
}
