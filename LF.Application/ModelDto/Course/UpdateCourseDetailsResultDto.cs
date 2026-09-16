namespace LF.Application.ModelDto.Course;

public sealed class UpdateCourseDetailsResultDto
{
    public CourseDetailDto Course { get; init; } = null!;

    // MinIO keys of a replaced cover image. LF.CourseService has no MinIO client, so it reports
    // them and LF.WebApi removes the blobs once the update has committed.
    public IReadOnlyList<string> OrphanedStorageObjectKeys { get; init; } = [];
}
