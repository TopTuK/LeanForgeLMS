using LF.AppDomain.Entities.Course;
using LF.AppDomain.Models.Course.Enums;

namespace LF.AppDomainTests.Entities.CourseAggregate;

public class CourseContentChangeTests
{
    private static readonly DateTime Now = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Lesson CreateLesson()
    {
        var course = Course.Create("Course", "Short", "Description", Category.Create("Backend"), 1, Now);
        return course.AddChapter("Chapter").AddLesson("Lesson");
    }

    [Fact]
    public void Create_SetsPendingChange()
    {
        var lesson = CreateLesson();

        var change = CourseContentChange.Create(5, lesson, CourseContentChangeKind.LessonAdded, Now);

        Assert.Equal(5, change.CourseId);
        Assert.Same(lesson, change.Lesson);
        Assert.Equal(Now, change.FirstChangedAt);
        Assert.Equal(Now, change.LastChangedAt);
        Assert.True(change.IsPending);
    }

    [Fact]
    public void Touch_KeepsAddedKindAndMovesLastChange()
    {
        var change = CourseContentChange.Create(5, CreateLesson(), CourseContentChangeKind.LessonAdded, Now);

        change.Touch(CourseContentChangeKind.LessonUpdated, Now.AddMinutes(10));

        Assert.Equal(CourseContentChangeKind.LessonAdded, change.Kind);
        Assert.Equal(Now, change.FirstChangedAt);
        Assert.Equal(Now.AddMinutes(10), change.LastChangedAt);
    }

    [Fact]
    public void Touch_NeverMovesLastChangeBackwards()
    {
        var change = CourseContentChange.Create(5, CreateLesson(), CourseContentChangeKind.LessonUpdated, Now);

        change.Touch(CourseContentChangeKind.LessonUpdated, Now.AddMinutes(-10));

        Assert.Equal(Now, change.LastChangedAt);
    }

    [Fact]
    public void MarkNotified_ClosesChange()
    {
        var change = CourseContentChange.Create(5, CreateLesson(), CourseContentChangeKind.LessonUpdated, Now);

        change.MarkNotified(Now.AddHours(1));

        Assert.False(change.IsPending);
        Assert.Throws<InvalidOperationException>(() => change.MarkNotified(Now.AddHours(2)));
        Assert.Throws<InvalidOperationException>(() => change.Touch(CourseContentChangeKind.LessonUpdated, Now.AddHours(2)));
    }

    [Fact]
    public void Create_InvalidCourseId_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CourseContentChange.Create(0, CreateLesson(), CourseContentChangeKind.LessonAdded, Now));
    }
}
