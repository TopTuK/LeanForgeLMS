using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomain.Models.Storage.Enums;

namespace LF.AppDomainTests.Entities.CourseAggregate;

public class LessonPartTests
{
    private static Lesson CreateLesson()
    {
        var category = Category.Create("Backend");
        var course = Course.Create("Title", "Short intro", "Description", category, 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        return chapter.AddLesson("Lesson 1", "Initial content", includeInPreview: false);
    }

    private static StorageObject CreateStorageObject(StorageObjectType type = StorageObjectType.Image) =>
        StorageObject.Create(type, "images/abc.png", "image/png", 1024, 1, DateTime.UtcNow);

    [Fact]
    public void ReplaceParts_TextPartWithEmptyHtml_Throws()
    {
        // Arrange
        var lesson = CreateLesson();
        var parts = new[] { new LessonPartInput(LessonPartType.Text, "  ", null) };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => lesson.ReplaceParts(parts));
    }

    [Fact]
    public void ReplaceParts_MediaPartWithoutStorageObject_Throws()
    {
        // Arrange
        var lesson = CreateLesson();
        var parts = new[] { new LessonPartInput(LessonPartType.Image, null, null) };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => lesson.ReplaceParts(parts));
    }

    [Fact]
    public void ReplaceParts_ValidMixedParts_AssignsSequentialSortOrder()
    {
        // Arrange
        var lesson = CreateLesson();
        var storageObject = CreateStorageObject();
        var parts = new[]
        {
            new LessonPartInput(LessonPartType.Text, "<p>Intro</p>", null),
            new LessonPartInput(LessonPartType.Image, null, storageObject),
        };

        // Act
        lesson.ReplaceParts(parts);

        // Assert
        Assert.Equal(2, lesson.Parts.Count);
        Assert.Equal(1, lesson.Parts[0].SortOrder);
        Assert.Equal(LessonPartType.Text, lesson.Parts[0].PartType);
        Assert.Equal(2, lesson.Parts[1].SortOrder);
        Assert.Equal(LessonPartType.Image, lesson.Parts[1].PartType);
        Assert.Same(storageObject, lesson.Parts[1].StorageObject);
    }

    [Fact]
    public void ReplaceParts_CalledTwice_ReplacesPreviousSet()
    {
        // Arrange
        var lesson = CreateLesson();
        lesson.ReplaceParts([new LessonPartInput(LessonPartType.Text, "<p>First</p>", null)]);

        // Act
        lesson.ReplaceParts([new LessonPartInput(LessonPartType.Text, "<p>Second</p>", null)]);

        // Assert
        Assert.Single(lesson.Parts);
        Assert.Equal("<p>Second</p>", lesson.Parts[0].Html);
    }
}
