using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomain.Models.Storage.Enums;
using LF.AppDomainTests.TestSupport;

namespace LF.AppDomainTests.Entities.CourseAggregate;

public class CourseTests
{
    private static Category CreateCategory() => Category.Create("Backend");

    private static Course CreateCourse() =>
        Course.Create("Title", "Short intro", "Description", CreateCategory(), 1, DateTime.UtcNow);

    private static StorageObject CreateStorageObject() =>
        StorageObject.Create(StorageObjectType.Image, "images/a.png", "image/png", 100, 1, DateTime.UtcNow);

    [Fact]
    public void Create_ValidArgs_SetsCreatedByUserId()
    {
        // Act
        var course = CreateCourse();

        // Assert
        Assert.Equal(1, course.CreatedByUserId);
        Assert.False(course.IsPublished);
        Assert.Empty(course.Chapters);
    }

    [Fact]
    public void Create_NonPositiveCreatedByUserId_Throws()
    {
        // Arrange
        var category = CreateCategory();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Course.Create("Title", "Short intro", "Description", category, 0, DateTime.UtcNow));
    }

    [Fact]
    public void Create_Free_DefaultsAndIgnoresPrice()
    {
        var course = Course.Create("Title", "Short intro", "Description", CreateCategory(), 1, DateTime.UtcNow,
            CoursePricingType.Free, price: 999m, CourseEnrollmentMode.Open);

        Assert.Equal(CoursePricingType.Free, course.PricingType);
        Assert.Null(course.Price);
        Assert.Equal(CourseEnrollmentMode.Open, course.EnrollmentMode);
    }

    [Fact]
    public void Create_Paid_RequiresPositivePrice()
    {
        Assert.Throws<ArgumentException>(() =>
            Course.Create("Title", "Short intro", "Description", CreateCategory(), 1, DateTime.UtcNow,
                CoursePricingType.Paid, price: null, CourseEnrollmentMode.Open));
    }

    [Fact]
    public void Create_Paid_RoundsAndStoresPrice()
    {
        var course = Course.Create("Title", "Short intro", "Description", CreateCategory(), 1, DateTime.UtcNow,
            CoursePricingType.Paid, price: 1990.126m, CourseEnrollmentMode.Managed);

        Assert.Equal(CoursePricingType.Paid, course.PricingType);
        Assert.Equal(1990.13m, course.Price);
        Assert.Equal(CourseEnrollmentMode.Managed, course.EnrollmentMode);
    }

    [Fact]
    public void AddChapter_IncrementsSortOrder()
    {
        // Arrange
        var course = CreateCourse();

        // Act
        var first = course.AddChapter("Chapter 1");
        var second = course.AddChapter("Chapter 2");

        // Assert
        Assert.Equal(1, first.SortOrder);
        Assert.Equal(2, second.SortOrder);
    }

    [Fact]
    public void Publish_NoChapters_Throws()
    {
        // Arrange
        var course = CreateCourse();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(course.Publish);
    }

    [Fact]
    public void Publish_ChapterWithoutLessons_Throws()
    {
        // Arrange
        var course = CreateCourse();
        course.AddChapter("Chapter 1");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(course.Publish);
    }

    [Fact]
    public void Publish_Valid_SetsIsPublished()
    {
        // Arrange
        var course = CreateCourse();
        var chapter = course.AddChapter("Chapter 1");
        chapter.AddLesson("Lesson 1");

        // Act
        course.Publish();

        // Assert
        Assert.True(course.IsPublished);
    }

    [Fact]
    public void MoveChapterUp_SwapsSortOrderAndListPosition()
    {
        // Arrange
        var course = CreateCourse();
        var first = course.AddChapter("Chapter 1");
        var second = course.AddChapter("Chapter 2");
        EntityIdSetter.SetId(first, 1);
        EntityIdSetter.SetId(second, 2);

        // Act
        course.MoveChapterUp(second.Id);

        // Assert
        Assert.Same(second, course.Chapters[0]);
        Assert.Same(first, course.Chapters[1]);
        Assert.Equal(1, second.SortOrder);
        Assert.Equal(2, first.SortOrder);
    }

    [Fact]
    public void MoveChapterUp_AtTop_NoOp()
    {
        // Arrange
        var course = CreateCourse();
        var first = course.AddChapter("Chapter 1");
        var second = course.AddChapter("Chapter 2");
        EntityIdSetter.SetId(first, 1);
        EntityIdSetter.SetId(second, 2);

        // Act
        course.MoveChapterUp(first.Id);

        // Assert
        Assert.Same(first, course.Chapters[0]);
        Assert.Equal(1, first.SortOrder);
    }

    [Fact]
    public void MoveChapterDown_AtBottom_NoOp()
    {
        // Arrange
        var course = CreateCourse();
        var first = course.AddChapter("Chapter 1");
        var second = course.AddChapter("Chapter 2");
        EntityIdSetter.SetId(first, 1);
        EntityIdSetter.SetId(second, 2);

        // Act
        course.MoveChapterDown(second.Id);

        // Assert
        Assert.Same(second, course.Chapters[1]);
        Assert.Equal(2, second.SortOrder);
    }

    [Fact]
    public void MoveChapterUp_UnknownId_Throws()
    {
        // Arrange
        var course = CreateCourse();
        var chapter = course.AddChapter("Chapter 1");
        EntityIdSetter.SetId(chapter, 1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => course.MoveChapterUp(999));
    }

    [Fact]
    public void Create_DefaultsToNoCover()
    {
        // Act
        var course = CreateCourse();

        // Assert
        Assert.Equal(CourseCoverType.None, course.CoverType);
        Assert.Null(course.CoverColor);
        Assert.Null(course.CoverImageStorageObjectId);
    }

    [Fact]
    public void SetColorCover_SetsColorAndClearsImage()
    {
        // Arrange
        var course = CreateCourse();
        course.SetImageCover(CreateStorageObject());

        // Act
        course.SetColorCover(CourseCoverColor.Ocean);

        // Assert
        Assert.Equal(CourseCoverType.Color, course.CoverType);
        Assert.Equal(CourseCoverColor.Ocean, course.CoverColor);
        Assert.Null(course.CoverImageStorageObjectId);
        Assert.Null(course.CoverImageStorageObject);
    }

    [Fact]
    public void SetImageCover_SetsImageAndClearsColor()
    {
        // Arrange
        var course = CreateCourse();
        course.SetColorCover(CourseCoverColor.Ocean);
        var storageObject = CreateStorageObject();
        EntityIdSetter.SetId(storageObject, 5);

        // Act
        course.SetImageCover(storageObject);

        // Assert
        Assert.Equal(CourseCoverType.Image, course.CoverType);
        Assert.Equal(5, course.CoverImageStorageObjectId);
        Assert.Same(storageObject, course.CoverImageStorageObject);
        Assert.Null(course.CoverColor);
    }

    [Fact]
    public void ClearCover_ResetsToNone()
    {
        // Arrange
        var course = CreateCourse();
        course.SetColorCover(CourseCoverColor.Ocean);

        // Act
        course.ClearCover();

        // Assert
        Assert.Equal(CourseCoverType.None, course.CoverType);
        Assert.Null(course.CoverColor);
        Assert.Null(course.CoverImageStorageObjectId);
    }
}
