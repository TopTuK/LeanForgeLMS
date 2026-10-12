using LF.AppDomain.Entities.User;
using LF.AppDomain.Models.Course.Enums;
using LF.Application.Common.Documents;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Course;
using LF.Application.ModelDto.Enrollment;
using LF.Application.Services.CourseExport;
using LF.Application.Services.EnrollmentLearning;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace LF.ApplicationTests.Services.CourseExport;

public class CourseExportServiceTests
{
    private const int StudentId = 7;
    private const int EnrollmentId = 42;

    private readonly Mock<IEnrollmentLearningService> _learning = new();
    private readonly Mock<IFileStorageService> _storage = new();
    private readonly Mock<ICoursePdfRenderer> _renderer = new();
    private CourseDocument? _rendered;

    private CourseExportService CreateService()
    {
        var dbContext = new Mock<IAppDbContext>();
        dbContext.SetupGet(c => c.Users).Returns(new List<DbUser>
        {
            new() { Id = StudentId, Email = "student@lf.test", FirstName = "Саша", LastName = "Студент" },
        }.BuildMockDbSet().Object);

        _renderer.Setup(r => r.Render(It.IsAny<CourseDocument>()))
            .Callback<CourseDocument>(d => _rendered = d)
            .Returns([0x25, 0x50, 0x44, 0x46]);

        return new CourseExportService(
            NullLogger<CourseExportService>.Instance,
            _learning.Object,
            dbContext.Object,
            _storage.Object,
            _renderer.Object,
            Clock());
    }

    private static TimeProvider Clock()
    {
        var clock = new Mock<TimeProvider>();
        clock.Setup(c => c.GetUtcNow()).Returns(new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.Zero));
        return clock.Object;
    }

    private void SetupEnrollment(EnrollmentDetailDto? enrollment)
        => _learning.Setup(s => s.GetEnrollmentAsync(EnrollmentId, StudentId, false)).ReturnsAsync(enrollment);

    private static EnrollmentDetailDto Enrollment(params EnrollmentChapterDto[] chapters) => new()
    {
        Id = EnrollmentId,
        CourseTitle = "Основы C#: курс",
        CourseDescription = "<p>Описание</p>",
        Chapters = chapters,
    };

    private static EnrollmentChapterDto Chapter(string title, int sortOrder, params EnrollmentLessonDto[] lessons)
        => new() { Title = title, SortOrder = sortOrder, Lessons = lessons };

    private static EnrollmentLessonDto Lesson(string title, int sortOrder, params LessonPartDto[] parts)
        => new() { Title = title, SortOrder = sortOrder, Parts = parts };

    [Fact]
    public async Task ExportEnrollmentPdfAsync_MissingEnrollment_ReturnsNull()
    {
        // Arrange
        SetupEnrollment(null);
        var service = CreateService();

        // Act
        var result = await service.ExportEnrollmentPdfAsync(EnrollmentId, StudentId, false, "ru", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
        _renderer.Verify(r => r.Render(It.IsAny<CourseDocument>()), Times.Never);
    }

    [Fact]
    public async Task ExportEnrollmentPdfAsync_ForeignEnrollment_PropagatesAuthorizationException()
    {
        // Arrange
        _learning.Setup(s => s.GetEnrollmentAsync(EnrollmentId, StudentId, false))
            .ThrowsAsync(new EnrollmentAuthorizationException("not yours"));
        var service = CreateService();

        // Act / Assert
        await Assert.ThrowsAsync<EnrollmentAuthorizationException>(
            () => service.ExportEnrollmentPdfAsync(EnrollmentId, StudentId, false, "ru", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExportEnrollmentPdfAsync_UnpublishedCourse_ThrowsCourseUnavailable()
    {
        // Arrange
        SetupEnrollment(new EnrollmentDetailDto { CourseTitle = "x", CourseDescription = "", IsCourseUnavailable = true });
        var service = CreateService();

        // Act / Assert
        await Assert.ThrowsAsync<CourseUnavailableException>(
            () => service.ExportEnrollmentPdfAsync(EnrollmentId, StudentId, false, "ru", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExportEnrollmentPdfAsync_OrdersChaptersAndLessonsAndNamesTheStudent()
    {
        // Arrange
        SetupEnrollment(Enrollment(
            Chapter("Second", 2, Lesson("B2", 2), Lesson("B1", 1)),
            Chapter("First", 1, Lesson("A1", 1))));
        var service = CreateService();

        // Act
        var result = await service.ExportEnrollmentPdfAsync(EnrollmentId, StudentId, false, "ru", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Основы-C#-курс.pdf", result.FileName);
        Assert.NotNull(_rendered);
        Assert.Equal(["First", "Second"], _rendered.Chapters.Select(c => c.Title));
        Assert.Equal(["B1", "B2"], _rendered.Chapters[1].Lessons.Select(l => l.Title));
        Assert.Equal("Саша Студент", _rendered.StudentName);
        Assert.IsType<ParagraphBlock>(Assert.Single(_rendered.Description));
    }

    [Fact]
    public async Task ExportEnrollmentPdfAsync_KeepsTextAndImagesAndSkipsOtherPartTypes()
    {
        // Arrange
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        _storage.Setup(s => s.DownloadAsync("img/1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new FileDownloadResult(new MemoryStream(imageBytes), "image/png"));

        SetupEnrollment(Enrollment(Chapter("Ch", 1, Lesson("L", 1,
            new LessonPartDto { PartType = LessonPartType.Quiz, SortOrder = 4 },
            new LessonPartDto
            {
                PartType = LessonPartType.Image,
                SortOrder = 2,
                Files = [
                new LessonPartFileDto { StorageObjectKey = "img/1", StorageObjectContentType = "image/png", SortOrder = 1 },
                new LessonPartFileDto { StorageObjectKey = "img/vector", StorageObjectContentType = "image/svg+xml", SortOrder = 2 },
            ]
            },
            new LessonPartDto { PartType = LessonPartType.Video, SortOrder = 3, StorageObjectKey = "video/1", StorageObjectContentType = "video/mp4" },
            new LessonPartDto { PartType = LessonPartType.Files, SortOrder = 5, Files = [new LessonPartFileDto { StorageObjectKey = "doc/1" }] },
            new LessonPartDto { PartType = LessonPartType.Text, SortOrder = 1, Html = "<p>Intro</p>" }))));
        var service = CreateService();

        // Act
        await service.ExportEnrollmentPdfAsync(EnrollmentId, StudentId, false, "en", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(_rendered);
        var blocks = _rendered.Chapters[0].Lessons[0].Blocks;
        Assert.Collection(blocks,
            b => Assert.IsType<ParagraphBlock>(b),
            b => Assert.Equal(imageBytes, Assert.IsType<ImageBlock>(b).Data));
        _storage.Verify(s => s.DownloadAsync("img/1", It.IsAny<CancellationToken>()), Times.Once);
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExportEnrollmentPdfAsync_LessonWithoutParts_UsesLegacyContent()
    {
        // Arrange
        SetupEnrollment(Enrollment(Chapter("Ch", 1, new EnrollmentLessonDto { Title = "Old", Content = "<h2>Legacy</h2>" })));
        var service = CreateService();

        // Act
        await service.ExportEnrollmentPdfAsync(EnrollmentId, StudentId, false, null, TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<HeadingBlock>(Assert.Single(_rendered!.Chapters[0].Lessons[0].Blocks));
    }

    [Theory]
    [InlineData("en", "Contents", "Exported on")]
    [InlineData("ru", "Содержание", "Дата выгрузки")]
    [InlineData(null, "Содержание", "Дата выгрузки")]
    [InlineData("de", "Содержание", "Дата выгрузки")]
    public async Task ExportEnrollmentPdfAsync_LocalizesLabels(string? language, string contents, string exportedPrefix)
    {
        // Arrange
        SetupEnrollment(Enrollment());
        var service = CreateService();

        // Act
        await service.ExportEnrollmentPdfAsync(EnrollmentId, StudentId, false, language, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(contents, _rendered!.Labels.Contents);
        Assert.StartsWith(exportedPrefix, _rendered.Labels.ExportedOn);
        Assert.Contains("2026", _rendered.Labels.ExportedOn);
    }

    [Fact]
    public async Task ExportEnrollmentPdfAsync_CancelledRequest_NeverRenders()
    {
        // Arrange
        SetupEnrollment(Enrollment());
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act / Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ExportEnrollmentPdfAsync(EnrollmentId, StudentId, false, "ru", cts.Token));
        _renderer.Verify(r => r.Render(It.IsAny<CourseDocument>()), Times.Never);
    }

    [Fact]
    public async Task ExportEnrollmentPdfAsync_ConcurrentExports_RenderAtMostTheGateLimitAtOnce()
    {
        // Arrange — each render blocks until released, so overlapping exports pile up at the gate.
        SetupEnrollment(Enrollment());
        var service = CreateService();
        var release = new ManualResetEventSlim();
        var running = 0;
        var peak = 0;
        _renderer.Setup(r => r.Render(It.IsAny<CourseDocument>())).Returns(() =>
        {
            var now = Interlocked.Increment(ref running);
            InterlockedMax(ref peak, now);
            release.Wait(TimeSpan.FromSeconds(10));
            Interlocked.Decrement(ref running);
            return [0x25];
        });

        // Act
        var exports = Enumerable.Range(0, CourseExportService.MaxConcurrentRenders + 2)
            .Select(_ => Task.Run(() => service.ExportEnrollmentPdfAsync(EnrollmentId, StudentId, false, "ru", TestContext.Current.CancellationToken)))
            .ToList();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (Volatile.Read(ref running) < CourseExportService.MaxConcurrentRenders && DateTime.UtcNow < deadline)
            await Task.Delay(10, TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        release.Set();
        await Task.WhenAll(exports);

        // Assert
        Assert.Equal(CourseExportService.MaxConcurrentRenders, peak);
        _renderer.Verify(r => r.Render(It.IsAny<CourseDocument>()), Times.Exactly(exports.Count));
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        while ((current = Volatile.Read(ref target)) < value && Interlocked.CompareExchange(ref target, value, current) != current)
        {
        }
    }

    [Theory]
    [InlineData("Intro / Basics: part 1?", "Intro-Basics-part-1.pdf")]
    [InlineData("   ", "course.pdf")]
    [InlineData("***", "course.pdf")]
    public void ToFileName_StripsCharactersBrowsersReject(string title, string expected)
        => Assert.Equal(expected, CourseExportService.ToFileName(title));
}
