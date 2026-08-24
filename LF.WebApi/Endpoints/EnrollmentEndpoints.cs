using System.Security.Claims;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Course;
using LF.Application.ModelDto.Enrollment;
using LF.Application.Services.EnrollmentLearning;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomain.Models.User.Enums;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

namespace LF.WebApi.Endpoints;

public sealed class EnrollmentEndpoints : IEndpointGroup
{
    private const int DefaultPageSize = 20;

    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/enrollments").WithTags("Enrollments").RequireAuthorization();

        group.MapGet("/catalog", async Task<Results<Ok<PagedCourseCatalogResponse>, UnauthorizedHttpResult>>
            (int? page, int? pageSize, ClaimsPrincipal user, IEnrollmentLearningService enrollmentService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var effectivePage = page is > 0 ? page.Value : 1;
            var effectivePageSize = pageSize is > 0 ? pageSize.Value : DefaultPageSize;

            var result = await enrollmentService.BrowseCatalogAsync(effectivePage, effectivePageSize, userId.Value);
            return TypedResults.Ok(new PagedCourseCatalogResponse([.. result.Items.Select(ToCatalogItemResponse)], result.TotalCount, effectivePage, effectivePageSize));
        });

        group.MapPost("/", async Task<Results<Created<EnrollmentDetailResponse>, UnauthorizedHttpResult, ValidationProblem, Conflict<string>, ForbidHttpResult>>
            (EnrollRequest request, ClaimsPrincipal user, IEnrollmentLearningService enrollmentService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new EnrollRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            try
            {
                var enrollment = await enrollmentService.EnrollAsync(request.CourseId, userId.Value);
                return TypedResults.Created($"/api/enrollments/{enrollment.Id}", ToDetailResponse(enrollment));
            }
            catch (SelfEnrollmentException)
            {
                return TypedResults.Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
        });

        group.MapGet("/mine", async Task<Results<Ok<IReadOnlyList<EnrollmentSummaryResponse>>, UnauthorizedHttpResult>>
            (string? status, ClaimsPrincipal user, IEnrollmentLearningService enrollmentService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var statusFilter = Enum.TryParse<EnrollmentStatusFilter>(status, ignoreCase: true, out var parsed)
                ? parsed
                : EnrollmentStatusFilter.Active;

            var result = await enrollmentService.ListMyEnrollmentsAsync(userId.Value, statusFilter);
            return TypedResults.Ok<IReadOnlyList<EnrollmentSummaryResponse>>([.. result.Select(ToSummaryResponse)]);
        });

        group.MapGet("/{id:int}", async Task<Results<Ok<EnrollmentDetailResponse>, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int id, ClaimsPrincipal user, IEnrollmentLearningService enrollmentService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var isAdmin = user.IsInRole(nameof(UserRole.Admin));

            try
            {
                var enrollment = await enrollmentService.GetEnrollmentAsync(id, userId.Value, isAdmin);
                return enrollment is null ? TypedResults.NotFound() : TypedResults.Ok(ToDetailResponse(enrollment));
            }
            catch (EnrollmentAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapPost("/{id:int}/lessons/{lessonId:int}/complete", async Task<Results<Ok<EnrollmentDetailResponse>, UnauthorizedHttpResult, NotFound, ForbidHttpResult, Conflict<string>>>
            (int id, int lessonId, ClaimsPrincipal user, IEnrollmentLearningService enrollmentService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var isAdmin = user.IsInRole(nameof(UserRole.Admin));

            try
            {
                var enrollment = await enrollmentService.CompleteLessonAsync(id, lessonId, userId.Value, isAdmin);
                return enrollment is null ? TypedResults.NotFound() : TypedResults.Ok(ToDetailResponse(enrollment));
            }
            catch (EnrollmentAuthorizationException)
            {
                return TypedResults.Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
        });

        group.MapPost("/{id:int}/lessons/{lessonId:int}/parts/{partId:int}/quiz/submit", async Task<Results<Ok<QuizSubmissionResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, ForbidHttpResult, Conflict<string>>>
            (int id, int lessonId, int partId, SubmitQuizAttemptRequest request, ClaimsPrincipal user, IEnrollmentLearningService enrollmentService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new SubmitQuizAttemptRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            var isAdmin = user.IsInRole(nameof(UserRole.Admin));
            var answers = request.Answers.Select(a => new QuizAnswerInputDto { QuestionId = a.QuestionId, SelectedOptionIds = a.SelectedOptionIds }).ToList();

            try
            {
                var submission = await enrollmentService.SubmitQuizAttemptAsync(id, lessonId, partId, answers, userId.Value, isAdmin);
                return submission is null ? TypedResults.NotFound() : TypedResults.Ok(ToQuizSubmissionResponse(submission));
            }
            catch (EnrollmentAuthorizationException)
            {
                return TypedResults.Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
        });

        group.MapGet("/{id:int}/lessons/{lessonId:int}/parts/{partId:int}/media", async Task<Results<FileStreamHttpResult, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int id, int lessonId, int partId, ClaimsPrincipal user, IEnrollmentLearningService enrollmentService,
             [FromKeyedServices("storage")] IFileStorageService fileStorageService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var isAdmin = user.IsInRole(nameof(UserRole.Admin));

            EnrollmentDetailDto? enrollment;
            try
            {
                enrollment = await enrollmentService.GetEnrollmentAsync(id, userId.Value, isAdmin);
            }
            catch (EnrollmentAuthorizationException)
            {
                return TypedResults.Forbid();
            }

            var part = enrollment?.Chapters.SelectMany(ch => ch.Lessons).FirstOrDefault(l => l.Id == lessonId)?.Parts.FirstOrDefault(p => p.Id == partId);
            if (part?.StorageObjectKey is null) return TypedResults.NotFound();

            var download = await fileStorageService.DownloadAsync(part.StorageObjectKey, ct);
            return download is null ? TypedResults.NotFound() : TypedResults.Stream(download.Content, download.ContentType);
        });

        group.MapGet("/{id:int}/lessons/{lessonId:int}/parts/{partId:int}/files/{fileId:int}/media", async Task<Results<FileStreamHttpResult, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int id, int lessonId, int partId, int fileId, ClaimsPrincipal user, IEnrollmentLearningService enrollmentService,
             [FromKeyedServices("storage")] IFileStorageService fileStorageService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var isAdmin = user.IsInRole(nameof(UserRole.Admin));

            EnrollmentDetailDto? enrollment;
            try
            {
                enrollment = await enrollmentService.GetEnrollmentAsync(id, userId.Value, isAdmin);
            }
            catch (EnrollmentAuthorizationException)
            {
                return TypedResults.Forbid();
            }

            var file = enrollment?.Chapters.SelectMany(ch => ch.Lessons).FirstOrDefault(l => l.Id == lessonId)
                ?.Parts.FirstOrDefault(p => p.Id == partId)?.Files.FirstOrDefault(f => f.Id == fileId);
            if (file is null) return TypedResults.NotFound();

            var download = await fileStorageService.DownloadAsync(file.StorageObjectKey, ct);
            return download is null ? TypedResults.NotFound() : TypedResults.File(download.Content, download.ContentType, fileDownloadName: file.FileName);
        });

        group.MapGet("/courses/{courseId:int}/cover/image", async Task<Results<FileStreamHttpResult, UnauthorizedHttpResult, NotFound>>
            (int courseId, ClaimsPrincipal user, IEnrollmentLearningService enrollmentService, [FromKeyedServices("storage")] IFileStorageService fileStorageService, CancellationToken ct) =>
        {
            if (user.GetUserId() is null) return TypedResults.Unauthorized();

            var cover = await enrollmentService.GetCourseCoverAsync(courseId);
            if (cover is null) return TypedResults.NotFound();

            var download = await fileStorageService.DownloadAsync(cover.CoverImageKey, ct);
            return download is null ? TypedResults.NotFound() : TypedResults.Stream(download.Content, download.ContentType);
        });
    }

    private static CourseCatalogItemResponse ToCatalogItemResponse(CourseCatalogItemDto item) => new(
        item.Id,
        item.Title,
        item.ShortIntroduction,
        item.CategoryId,
        item.CategoryName,
        item.LessonCount,
        item.CoverType.ToString(),
        item.CoverColor?.ToString(),
        item.CoverType == CourseCoverType.Image ? $"/api/enrollments/courses/{item.Id}/cover/image" : null);

    private static EnrollmentSummaryResponse ToSummaryResponse(EnrollmentSummaryDto dto) => new(
        dto.Id,
        dto.CourseId,
        dto.CourseTitle,
        dto.CourseShortIntroduction,
        dto.CategoryName,
        dto.TotalLessonCount,
        dto.CompletedLessonCount,
        dto.ProgressPercent,
        dto.EnrolledAt,
        dto.CompletedAt,
        dto.CoverType.ToString(),
        dto.CoverColor?.ToString(),
        dto.CoverType == CourseCoverType.Image ? $"/api/enrollments/courses/{dto.CourseId}/cover/image" : null);

    private static QuizSubmissionResponse ToQuizSubmissionResponse(QuizSubmissionDto dto) => new(
        new QuizAttemptResultResponse(
            dto.Result.ScorePercent,
            dto.Result.Passed,
            [.. dto.Result.Questions.Select(q => new QuizQuestionResultResponse(q.QuestionId, q.IsCorrect, q.CorrectOptionIds))]),
        ToDetailResponse(dto.Enrollment));

    private static EnrollmentDetailResponse ToDetailResponse(EnrollmentDetailDto dto) => new(
        dto.Id,
        dto.CourseId,
        dto.CourseTitle,
        dto.CourseDescription,
        dto.EnrolledAt,
        dto.CompletedAt,
        [.. dto.Chapters.Select(ch => ToChapterResponse(dto.Id, ch))]);

    private static EnrollmentChapterResponse ToChapterResponse(int enrollmentId, EnrollmentChapterDto chapter) => new(
        chapter.Id,
        chapter.Title,
        chapter.SortOrder,
        [.. chapter.Lessons.Select(l => ToLessonResponse(enrollmentId, l))]);

    private static EnrollmentLessonResponse ToLessonResponse(int enrollmentId, EnrollmentLessonDto lesson) => new(
        lesson.Id,
        lesson.Title,
        lesson.Content,
        lesson.SortOrder,
        lesson.IsCompleted,
        [.. lesson.Parts.Select(p => ToLessonPartResponse(enrollmentId, lesson.Id, p))]);

    // Correct answers must never reach a learner before they submit — IsCorrect/CorrectOptionIds are
    // deliberately nulled out here even though the underlying LessonPartDto carries the real values
    // (the authoring-side mapping in CourseEndpoints keeps them, since that path is owner/admin only).
    private static LessonPartResponse ToLessonPartResponse(int enrollmentId, int lessonId, LessonPartDto part) => new(
        part.Id,
        part.PartType.ToString(),
        part.SortOrder,
        part.Html,
        part.StorageObjectId,
        part.PartType is LessonPartType.Text or LessonPartType.Quiz or LessonPartType.Files
            ? null
            : $"/api/enrollments/{enrollmentId}/lessons/{lessonId}/parts/{part.Id}/media",
        part.PartType == LessonPartType.Quiz ? [.. part.QuizQuestions.Select(ToLearnerQuizQuestionResponse)] : null,
        part.PartType == LessonPartType.Quiz ? part.QuizPassThresholdPercent : null,
        part.PartType == LessonPartType.Files ? [.. part.Files.Select(f => ToLessonPartFileResponse(enrollmentId, lessonId, part.Id, f))] : null);

    private static LessonPartFileResponse ToLessonPartFileResponse(int enrollmentId, int lessonId, int partId, LessonPartFileDto file) => new(
        file.Id,
        file.FileName,
        file.StorageObjectId,
        file.StorageObjectSizeBytes,
        file.StorageObjectContentType,
        $"/api/enrollments/{enrollmentId}/lessons/{lessonId}/parts/{partId}/files/{file.Id}/media");

    private static QuizQuestionResponse ToLearnerQuizQuestionResponse(QuizQuestionDto question) => new(
        question.Id,
        question.Text,
        question.QuestionType.ToString(),
        question.SortOrder,
        [.. question.Options.Select(o => new QuizOptionResponse(o.Id, o.Text, null, o.SortOrder))]);
}
