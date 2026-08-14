using LF.AppDomain.Entities.Course;

namespace LF.AppDomainTests.Entities.CourseAggregate;

public class LessonTests
{
    private static Lesson CreateLesson()
    {
        var category = Category.Create("Backend");
        var course = Course.Create("Title", "Short intro", "Description", category, 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        return chapter.AddLesson("Lesson 1", "Initial content", includeInPreview: false);
    }

    [Fact]
    public void UpdateContent_NullClearsToEmptyString()
    {
        // Arrange
        var lesson = CreateLesson();

        // Act
        lesson.UpdateContent(null);

        // Assert
        Assert.Equal(string.Empty, lesson.Content);
    }

    [Fact]
    public void UpdateContent_SetsNewContent()
    {
        // Arrange
        var lesson = CreateLesson();

        // Act
        lesson.UpdateContent("Updated content");

        // Assert
        Assert.Equal("Updated content", lesson.Content);
    }

    [Fact]
    public void SetIncludeInPreview_TogglesFlag()
    {
        // Arrange
        var lesson = CreateLesson();

        // Act
        lesson.SetIncludeInPreview(true);

        // Assert
        Assert.True(lesson.IncludeInPreview);
    }

    [Fact]
    public void Rename_EmptyTitle_Throws()
    {
        // Arrange
        var lesson = CreateLesson();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => lesson.Rename(""));
    }
}
