using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomain.Models.Storage.Enums;
using LF.Application.Common.Mapping;
using LF.Application.ModelDto.Course;
using Mapster;
using DomainCourse = LF.AppDomain.Entities.Course.Course;

namespace LF.ApplicationTests.Common.Mapping;

public class CourseMappingConfigTests
{
    private static TypeAdapterConfig CreateConfig()
    {
        var config = new TypeAdapterConfig();
        new CourseMappingConfig().Register(config);
        return config;
    }

    [Fact]
    public void Adapt_CourseToCourseDetailDto_MapsCategoryNameAndChaptersOrderedBySortOrder()
    {
        // Arrange
        var config = CreateConfig();
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        chapter.AddLesson("Lesson 1");
        chapter.AddLesson("Lesson 2");

        // Act
        var result = course.Adapt<CourseDetailDto>(config);

        // Assert
        Assert.Equal("Backend", result.CategoryName);
        Assert.Single(result.Chapters);
        Assert.Equal(2, result.Chapters[0].Lessons.Count);
        Assert.Equal("Lesson 1", result.Chapters[0].Lessons[0].Title);
        Assert.Equal("Lesson 2", result.Chapters[0].Lessons[1].Title);
    }

    [Fact]
    public void Adapt_CourseToCourseSummaryDto_MapsChapterCount()
    {
        // Arrange
        var config = CreateConfig();
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        course.AddChapter("Chapter 1");
        course.AddChapter("Chapter 2");

        // Act
        var result = course.Adapt<CourseSummaryDto>(config);

        // Assert
        Assert.Equal("Backend", result.CategoryName);
        Assert.Equal(2, result.ChapterCount);
    }

    [Fact]
    public void Adapt_CourseToCourseDetailDto_MapsLessonPartsOrderedBySortOrderWithStorageObjectInfo()
    {
        // Arrange
        var config = CreateConfig();
        var category = Category.Create("Backend");
        var course = DomainCourse.Create("Title", "Short", "Description", category, createdByUserId: 1, DateTime.UtcNow);
        var chapter = course.AddChapter("Chapter 1");
        var lesson = chapter.AddLesson("Lesson 1");
        var storageObject = StorageObject.Create(StorageObjectType.Image, "images/abc.png", "image/png", 1024, 1, DateTime.UtcNow);
        lesson.ReplaceParts([
            new LessonPartInput(LessonPartType.Image, null, storageObject),
            new LessonPartInput(LessonPartType.Text, "<p>Intro</p>", null),
        ]);

        // Act
        var result = course.Adapt<CourseDetailDto>(config);

        // Assert
        var parts = result.Chapters[0].Lessons[0].Parts;
        Assert.Equal(2, parts.Count);
        Assert.Equal(LessonPartType.Image, parts[0].PartType);
        Assert.Equal("images/abc.png", parts[0].StorageObjectKey);
        Assert.Equal("image/png", parts[0].StorageObjectContentType);
        Assert.Equal(LessonPartType.Text, parts[1].PartType);
        Assert.Equal("<p>Intro</p>", parts[1].Html);
    }
}
