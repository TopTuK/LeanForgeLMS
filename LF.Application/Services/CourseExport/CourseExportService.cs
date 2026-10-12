using System.Globalization;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomain.Models.User;
using LF.Application.Common.Content;
using LF.Application.Common.Documents;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Course;
using LF.Application.ModelDto.CourseExport;
using LF.Application.ModelDto.Enrollment;
using LF.Application.Services.EnrollmentLearning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.CourseExport;

internal sealed class CourseExportService(
    ILogger<CourseExportService> logger,
    IEnrollmentLearningService enrollmentLearningService,
    IAppDbContext dbContext,
    [FromKeyedServices("storage")] IFileStorageService fileStorageService,
    ICoursePdfRenderer pdfRenderer,
    TimeProvider timeProvider) : ICourseExportService
{
    // Formats the PDF renderer can decode; anything else (e.g. SVG) is left out rather than failing the export.
    private static readonly HashSet<string> PrintableImageTypes =
        new(["image/png", "image/jpeg", "image/jpg", "image/webp", "image/gif", "image/bmp"], StringComparer.OrdinalIgnoreCase);

    internal const long MaxImageBytes = 10 * 1024 * 1024;

    private readonly ILogger<CourseExportService> _logger = logger;
    private readonly IEnrollmentLearningService _enrollmentLearningService = enrollmentLearningService;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly ICoursePdfRenderer _pdfRenderer = pdfRenderer;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<CourseExportFileDto?> ExportEnrollmentPdfAsync(int enrollmentId, int actingUserId, bool isAdmin, string? language, CancellationToken ct)
    {
        _logger.LogInformation("CourseExportService::ExportEnrollmentPdfAsync: called with EnrollmentId={EnrollmentId} ActingUserId={ActingUserId} Language={Language}",
            enrollmentId, actingUserId, language);

        // Same gate as the learn view: ownership, admin override and the pending-payment block.
        var enrollment = await _enrollmentLearningService.GetEnrollmentAsync(enrollmentId, actingUserId, isAdmin);
        if (enrollment is null)
            return null;

        if (enrollment.IsCourseUnavailable)
            throw new CourseUnavailableException("The course is no longer available.");

        var exportedAt = _timeProvider.GetUtcNow();
        var labels = CreateLabels(UserLanguage.OrDefault(language), exportedAt);

        var chapters = new List<DocumentChapter>(enrollment.Chapters.Count);
        foreach (var chapter in enrollment.Chapters.OrderBy(c => c.SortOrder))
        {
            var lessons = new List<DocumentLesson>(chapter.Lessons.Count);
            foreach (var lesson in chapter.Lessons.OrderBy(l => l.SortOrder))
                lessons.Add(new DocumentLesson(lesson.Title, await BuildLessonBlocksAsync(lesson, ct)));

            chapters.Add(new DocumentChapter(chapter.Title, lessons));
        }

        var document = new CourseDocument(
            enrollment.CourseTitle,
            LessonHtmlBlockParser.Parse(enrollment.CourseDescription),
            await GetStudentNameAsync(actingUserId, ct),
            exportedAt,
            labels,
            chapters);

        var content = _pdfRenderer.Render(document);

        _logger.LogInformation("CourseExportService::ExportEnrollmentPdfAsync: rendered EnrollmentId={EnrollmentId} SizeBytes={SizeBytes}",
            enrollmentId, content.Length);

        return new CourseExportFileDto(content, ToFileName(enrollment.CourseTitle));
    }

    private async Task<IReadOnlyList<DocumentBlock>> BuildLessonBlocksAsync(EnrollmentLessonDto lesson, CancellationToken ct)
    {
        // Lessons authored before parts existed keep their body in Content.
        if (lesson.Parts.Count == 0)
            return LessonHtmlBlockParser.Parse(lesson.Content);

        var blocks = new List<DocumentBlock>();
        foreach (var part in lesson.Parts.OrderBy(p => p.SortOrder))
        {
            switch (part.PartType)
            {
                case LessonPartType.Text:
                    blocks.AddRange(LessonHtmlBlockParser.Parse(part.Html));
                    break;

                case LessonPartType.Image:
                    await foreach (var image in LoadImagesAsync(part, ct))
                        blocks.Add(image);
                    break;
            }
        }

        return blocks;
    }

    private async IAsyncEnumerable<ImageBlock> LoadImagesAsync(LessonPartDto part, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        IEnumerable<(string Key, string? ContentType)> sources = part.Files.Count > 0
            ? part.Files.OrderBy(f => f.SortOrder).Select(f => (f.StorageObjectKey, (string?)f.StorageObjectContentType))
            : part.StorageObjectKey is null ? [] : [(part.StorageObjectKey, part.StorageObjectContentType)];

        foreach (var (key, contentType) in sources)
        {
            if (contentType is not null && !PrintableImageTypes.Contains(contentType))
                continue;

            var image = await DownloadImageAsync(key, ct);
            if (image is not null)
                yield return new ImageBlock(image);
        }
    }

    private async Task<byte[]?> DownloadImageAsync(string objectKey, CancellationToken ct)
    {
        var download = await _fileStorageService.DownloadAsync(objectKey, ct);
        if (download is null)
        {
            _logger.LogWarning("CourseExportService::DownloadImageAsync: object {ObjectKey} not found, skipping", objectKey);
            return null;
        }

        await using var content = download.Content;
        if (!PrintableImageTypes.Contains(download.ContentType) || (content.CanSeek && content.Length > MaxImageBytes))
            return null;

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        return buffer.Length is 0 or > MaxImageBytes ? null : buffer.ToArray();
    }

    private async Task<string> GetStudentNameAsync(int userId, CancellationToken ct)
    {
        var user = await _dbContext.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.FirstName, u.LastName, u.Email })
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return string.Empty;

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return fullName.Length > 0 ? fullName : user.Email;
    }

    private static CourseDocumentLabels CreateLabels(string language, DateTimeOffset exportedAt)
    {
        var culture = CultureInfo.GetCultureInfo(language == UserLanguage.English ? "en-US" : "ru-RU");
        var date = exportedAt.ToString("D", culture);

        return language == UserLanguage.English
            ? new CourseDocumentLabels("Contents", "Chapter", "Prepared for", $"Exported on {date}")
            : new CourseDocumentLabels("Содержание", "Глава", "Подготовлено для", $"Дата выгрузки: {date}");
    }

    internal static string ToFileName(string courseTitle)
    {
        // Fixed set rather than Path.GetInvalidFileNameChars(): that one is OS-specific, and the name is saved on the client.
        const string invalid = "\\/:*?\"<>|";
        var cleaned = new string([.. courseTitle.Select(c => invalid.Contains(c) || char.IsControl(c) ? ' ' : c)]);
        var collapsed = string.Join('-', cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (collapsed.Length > 80)
            collapsed = collapsed[..80].TrimEnd('-', '.');

        return (collapsed.Length == 0 ? "course" : collapsed) + ".pdf";
    }
}
