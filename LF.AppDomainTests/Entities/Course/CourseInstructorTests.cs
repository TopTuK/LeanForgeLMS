using LF.AppDomain.Entities.Course;

namespace LF.AppDomainTests.Entities.CourseAggregate;

public class CourseInstructorTests
{
    private static readonly DateTime Now = new(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_SetsAssignment()
    {
        var assignment = CourseInstructor.Create(courseId: 7, userId: 9, assignedByUserId: 3, Now);

        Assert.Equal(7, assignment.CourseId);
        Assert.Equal(9, assignment.UserId);
        Assert.Equal(3, assignment.AssignedByUserId);
        Assert.Equal(Now, assignment.AssignedAt);
    }

    [Theory]
    [InlineData(0, 9, 3)]
    [InlineData(7, 0, 3)]
    [InlineData(7, 9, 0)]
    [InlineData(-1, 9, 3)]
    public void Create_NonPositiveIdentifiers_Throw(int courseId, int userId, int assignedByUserId) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CourseInstructor.Create(courseId, userId, assignedByUserId, Now));
}
