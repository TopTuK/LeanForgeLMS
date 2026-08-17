using System.Security.Claims;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
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

        group.MapPost("/", async Task<Results<Created<EnrollmentDetailResponse>, UnauthorizedHttpResult, ValidationProblem, Conflict<string>>>
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

    private static EnrollmentDetailResponse ToDetailResponse(EnrollmentDetailDto dto) => new(
        dto.Id,
        dto.CourseId,
        dto.CourseTitle,
        dto.CourseDescription,
        dto.EnrolledAt,
        dto.CompletedAt,
        [.. dto.Chapters.Select(ToChapterResponse)]);

    private static EnrollmentChapterResponse ToChapterResponse(EnrollmentChapterDto chapter) => new(
        chapter.Id,
        chapter.Title,
        chapter.SortOrder,
        [.. chapter.Lessons.Select(l => new EnrollmentLessonResponse(l.Id, l.Title, l.Content, l.SortOrder, l.IsCompleted))]);
}
