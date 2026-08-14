using LF.AppDomain.Entities.Course;
using LF.AppDomainTests.TestSupport;

namespace LF.AppDomainTests.Entities.CourseAggregate;

public class ChapterTests
{
    private static Course CreateCourseWithChapter(out Chapter chapter)
    {
        var category = Category.Create("Backend");
        var course = Course.Create("Title", "Short intro", "Description", category, 1, DateTime.UtcNow);
        chapter = course.AddChapter("Chapter 1");
        return course;
    }

    [Fact]
    public void AddLesson_IncrementsSortOrder()
    {
        // Arrange
        CreateCourseWithChapter(out var chapter);

        // Act
        var first = chapter.AddLesson("Lesson 1");
        var second = chapter.AddLesson("Lesson 2");

        // Assert
        Assert.Equal(1, first.SortOrder);
        Assert.Equal(2, second.SortOrder);
    }

    [Fact]
    public void MoveLessonUp_SwapsSortOrderAndListPosition()
    {
        // Arrange
        CreateCourseWithChapter(out var chapter);
        var first = chapter.AddLesson("Lesson 1");
        var second = chapter.AddLesson("Lesson 2");
        EntityIdSetter.SetId(first, 1);
        EntityIdSetter.SetId(second, 2);

        // Act
        chapter.MoveLessonUp(second.Id);

        // Assert
        Assert.Same(second, chapter.Lessons[0]);
        Assert.Same(first, chapter.Lessons[1]);
        Assert.Equal(1, second.SortOrder);
        Assert.Equal(2, first.SortOrder);
    }

    [Fact]
    public void MoveLessonDown_AtBottom_NoOp()
    {
        // Arrange
        CreateCourseWithChapter(out var chapter);
        var first = chapter.AddLesson("Lesson 1");
        var second = chapter.AddLesson("Lesson 2");
        EntityIdSetter.SetId(first, 1);
        EntityIdSetter.SetId(second, 2);

        // Act
        chapter.MoveLessonDown(second.Id);

        // Assert
        Assert.Same(second, chapter.Lessons[1]);
        Assert.Equal(2, second.SortOrder);
    }

    [Fact]
    public void RemoveLesson_RenumbersRemaining()
    {
        // Arrange
        CreateCourseWithChapter(out var chapter);
        var first = chapter.AddLesson("Lesson 1");
        var second = chapter.AddLesson("Lesson 2");
        var third = chapter.AddLesson("Lesson 3");
        EntityIdSetter.SetId(first, 1);
        EntityIdSetter.SetId(second, 2);
        EntityIdSetter.SetId(third, 3);

        // Act
        chapter.RemoveLesson(second.Id);

        // Assert
        Assert.Equal(2, chapter.Lessons.Count);
        Assert.Same(first, chapter.Lessons[0]);
        Assert.Same(third, chapter.Lessons[1]);
        Assert.Equal(1, first.SortOrder);
        Assert.Equal(2, third.SortOrder);
    }

    [Fact]
    public void RemoveLesson_UnknownId_Throws()
    {
        // Arrange
        CreateCourseWithChapter(out var chapter);
        var lesson = chapter.AddLesson("Lesson 1");
        EntityIdSetter.SetId(lesson, 1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => chapter.RemoveLesson(999));
    }

    [Fact]
    public void Rename_EmptyTitle_Throws()
    {
        // Arrange
        CreateCourseWithChapter(out var chapter);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => chapter.Rename("   "));
    }
}
