using System.Security.Claims;
using LF.AppDomain.Models.Course.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.ModelDto.Course;
using LF.Application.ModelDto.Enrollment;
using LF.Application.Services.Admin;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

public sealed class AdminCourseEndpoints : IEndpointGroup
{
    private const int DefaultPageSize = 20;

    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/courses").WithTags("AdminCourses").RequireAuthorization("AdminOnly");

        group.MapGet("/", async Task<Results<Ok<PagedAdminCoursesResponse>, UnauthorizedHttpResult>>
            (int? page, int? pageSize, ClaimsPrincipal user, IAdminCourseService adminCourseService, CancellationToken ct) =>
        {
            var adminId = user.GetUserId();
            if (adminId is null) return TypedResults.Unauthorized();

            var effectivePage = page is > 0 ? page.Value : 1;
            var effectivePageSize = pageSize is > 0 ? pageSize.Value : DefaultPageSize;

            var result = await adminCourseService.ListCoursesAsync(effectivePage, effectivePageSize, adminId.Value);
            return TypedResults.Ok(new PagedAdminCoursesResponse(
                [.. result.Items.Select(ToSummaryResponse)],
                result.TotalCount,
                effectivePage,
                effectivePageSize));
        });

        group.MapGet("/{id:int}/enrollments", async Task<Results<Ok<PagedAdminCourseEnrollmentsResponse>, UnauthorizedHttpResult, NotFound>>
            (int id, int? page, int? pageSize, ClaimsPrincipal user, IAdminCourseService adminCourseService, CancellationToken ct) =>
        {
            var adminId = user.GetUserId();
            if (adminId is null) return TypedResults.Unauthorized();

            var effectivePage = page is > 0 ? page.Value : 1;
            var effectivePageSize = pageSize is > 0 ? pageSize.Value : DefaultPageSize;

            var result = await adminCourseService.ListCourseEnrollmentsAsync(id, adminId.Value, effectivePage, effectivePageSize);
            return result is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(new PagedAdminCourseEnrollmentsResponse(
                    [.. result.Items.Select(ToEnrollmentResponse)],
                    result.TotalCount,
                    effectivePage,
                    effectivePageSize));
        });

        group.MapPost("/{id:int}/enrollments", async Task<Results<Created<AdminCourseEnrollmentResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, Conflict<string>>>
            (int id, EnrollUserRequest request, ClaimsPrincipal user, IAdminCourseService adminCourseService, CancellationToken ct) =>
        {
            var adminId = user.GetUserId();
            if (adminId is null) return TypedResults.Unauthorized();

            var validation = new EnrollUserRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            try
            {
                var enrollment = await adminCourseService.EnrollStudentAsync(id, request.UserId, adminId.Value);
                return enrollment is null
                    ? TypedResults.NotFound()
                    : TypedResults.Created($"/api/admin/courses/{id}/enrollments", ToEnrollmentResponse(enrollment, request.UserId));
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
        });

        group.MapDelete("/{id:int}/enrollments/{enrollmentId:int}", async Task<Results<Ok<RemoveEnrollmentResponse>, UnauthorizedHttpResult, NotFound>>
            (int id, int enrollmentId, ClaimsPrincipal user, IAdminCourseService adminCourseService, CancellationToken ct) =>
        {
            var adminId = user.GetUserId();
            if (adminId is null) return TypedResults.Unauthorized();

            var result = await adminCourseService.RemoveEnrollmentAsync(id, enrollmentId, adminId.Value);
            return result is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(new RemoveEnrollmentResponse(result.UserId, result.WasPaid, result.PricePaid));
        });

        // force=true is the admin's explicit acknowledgement that paid students lose access
        // without a refund; without it a paid course refuses to delete.
        group.MapDelete("/{id:int}", async Task<Results<Ok<DeleteCourseResponse>, UnauthorizedHttpResult, NotFound, Conflict<string>>>
            (int id, bool? force, ClaimsPrincipal user, IAdminCourseService adminCourseService, CancellationToken ct) =>
        {
            var adminId = user.GetUserId();
            if (adminId is null) return TypedResults.Unauthorized();

            try
            {
                var result = await adminCourseService.DeleteCourseAsync(id, adminId.Value, force ?? false);
                return result is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok(new DeleteCourseResponse(result.RemovedEnrollmentCount, result.PaidEnrollmentCount));
            }
            catch (CourseDeletionBlockedException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
        });
    }

    private static AdminCourseSummaryResponse ToSummaryResponse(CourseSummaryDto course) => new(
        course.Id,
        course.Title,
        course.ShortIntroduction,
        course.IsPublished,
        course.CategoryId,
        course.CategoryName,
        course.CreatedByUserId,
        course.AuthorEmail,
        course.AuthorFirstName,
        course.AuthorLastName,
        course.CreatedAt,
        course.ChapterCount,
        course.PricingType.ToString(),
        course.Price,
        course.EnrollmentMode.ToString());

    private static AdminCourseEnrollmentResponse ToEnrollmentResponse(CourseEnrollmentDto dto) => new(
        dto.Id,
        dto.UserId,
        dto.StudentEmail,
        dto.StudentFirstName,
        dto.StudentLastName,
        dto.Status.ToString(),
        dto.PricePaid,
        dto.IsPaid,
        dto.EnrolledAt,
        dto.CompletedAt,
        dto.TotalLessonCount,
        dto.CompletedLessonCount,
        dto.ProgressPercent);

    // A fresh managed enrollment always comes back Active at zero price, so it can never be
    // "paid" — the SPA reloads the roster anyway, this just shapes the 201 body.
    private static AdminCourseEnrollmentResponse ToEnrollmentResponse(EnrollmentSummaryDto dto, int userId) => new(
        dto.Id,
        userId,
        null,
        null,
        null,
        dto.Status.ToString(),
        dto.PricePaid,
        false,
        dto.EnrolledAt,
        dto.CompletedAt,
        dto.TotalLessonCount,
        dto.CompletedLessonCount,
        dto.ProgressPercent);
}
