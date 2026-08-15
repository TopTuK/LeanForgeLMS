using LF.AppDomain.Entities.Course;

namespace LF.AppDomainTests.Entities.CourseAggregate;

public class EnrollmentTests
{
    [Fact]
    public void Create_ValidArgs_SetsFields()
    {
        // Arrange
        var enrolledAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var enrollment = Enrollment.Create(courseId: 1, userId: 2, enrolledAt);

        // Assert
        Assert.Equal(1, enrollment.CourseId);
        Assert.Equal(2, enrollment.UserId);
        Assert.Equal(enrolledAt, enrollment.EnrolledAt);
        Assert.Null(enrollment.CompletedAt);
        Assert.Empty(enrollment.CompletedLessonIds);
    }

    [Fact]
    public void Create_InvalidCourseId_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Enrollment.Create(courseId: 0, userId: 1, DateTime.UtcNow));
    }

    [Fact]
    public void Create_InvalidUserId_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Enrollment.Create(courseId: 1, userId: 0, DateTime.UtcNow));
    }

    [Fact]
    public void CompleteLesson_NewLesson_ReturnsTrueAndAdds()
    {
        // Arrange
        var enrollment = Enrollment.Create(1, 1, DateTime.UtcNow);

        // Act
        var changed = enrollment.CompleteLesson(10);

        // Assert
        Assert.True(changed);
        Assert.Contains(10, enrollment.CompletedLessonIds);
    }

    [Fact]
    public void CompleteLesson_AlreadyCompleted_ReturnsFalse()
    {
        // Arrange
        var enrollment = Enrollment.Create(1, 1, DateTime.UtcNow);
        enrollment.CompleteLesson(10);

        // Act
        var changed = enrollment.CompleteLesson(10);

        // Assert
        Assert.False(changed);
        Assert.Single(enrollment.CompletedLessonIds);
    }

    [Fact]
    public void RecalculateCompletion_AllLessonsComplete_SetsCompletedAt()
    {
        // Arrange
        var enrollment = Enrollment.Create(1, 1, DateTime.UtcNow);
        enrollment.CompleteLesson(1);
        enrollment.CompleteLesson(2);
        var now = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        enrollment.RecalculateCompletion(totalLessonCount: 2, now);

        // Assert
        Assert.Equal(now, enrollment.CompletedAt);
    }

    [Fact]
    public void RecalculateCompletion_BelowTotal_ClearsCompletedAt()
    {
        // Arrange
        var enrollment = Enrollment.Create(1, 1, DateTime.UtcNow);
        enrollment.CompleteLesson(1);
        enrollment.CompleteLesson(2);
        enrollment.RecalculateCompletion(totalLessonCount: 2, DateTime.UtcNow);

        // Act — an author added a third lesson after the enrollment had already completed
        enrollment.RecalculateCompletion(totalLessonCount: 3, DateTime.UtcNow);

        // Assert
        Assert.Null(enrollment.CompletedAt);
    }

    [Fact]
    public void RecalculateCompletion_ZeroLessons_DoesNotComplete()
    {
        // Arrange
        var enrollment = Enrollment.Create(1, 1, DateTime.UtcNow);

        // Act
        enrollment.RecalculateCompletion(totalLessonCount: 0, DateTime.UtcNow);

        // Assert
        Assert.Null(enrollment.CompletedAt);
    }

    [Theory]
    [InlineData(0, 5, 0)]
    [InlineData(2, 4, 50)]
    [InlineData(5, 5, 100)]
    [InlineData(1, 3, 33)]
    public void ProgressPercent_ComputesCorrectly(int completedCount, int totalLessonCount, int expectedPercent)
    {
        // Arrange
        var enrollment = Enrollment.Create(1, 1, DateTime.UtcNow);
        for (var i = 1; i <= completedCount; i++)
            enrollment.CompleteLesson(i);

        // Act
        var percent = enrollment.ProgressPercent(totalLessonCount);

        // Assert
        Assert.Equal(expectedPercent, percent);
    }

    [Fact]
    public void ProgressPercent_ZeroTotalLessons_ReturnsZero()
    {
        // Arrange
        var enrollment = Enrollment.Create(1, 1, DateTime.UtcNow);

        // Act
        var percent = enrollment.ProgressPercent(0);

        // Assert
        Assert.Equal(0, percent);
    }
}
