namespace LF.AppDomain.Entities.Course;

// Grants a user teaching rights on one course. The course creator is teaching staff by virtue of
// Course.CreatedByUserId and is never stored here, so a course's staff is always
// { CreatedByUserId } union { assigned UserIds }.
public sealed class CourseInstructor
{
    private CourseInstructor()
    {
    }

    public int Id { get; private set; }
    public int CourseId { get; private set; }
    public int UserId { get; private set; }
    public int AssignedByUserId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    public static CourseInstructor Create(int courseId, int userId, int assignedByUserId, DateTime assignedAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(courseId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(userId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(assignedByUserId, 0);

        return new CourseInstructor
        {
            CourseId = courseId,
            UserId = userId,
            AssignedByUserId = assignedByUserId,
            AssignedAt = assignedAt,
        };
    }
}
