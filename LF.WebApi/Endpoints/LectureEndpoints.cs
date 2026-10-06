using System.Security.Claims;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.Services.Lectures;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

// Scheduled online lectures. Staff-only management, per-course authorization in the service.
public sealed class LectureEndpoints : IEndpointGroup
{
    private static readonly TimeSpan DefaultSchedulePast = TimeSpan.FromDays(30);
    private static readonly TimeSpan DefaultScheduleFuture = TimeSpan.FromDays(120);

    public void Map(IEndpointRouteBuilder app)
    {
        var courseLectures = app.MapGroup("/api/courses/{courseId:int}/lectures").WithTags("Lectures").RequireAuthorization();
        var lectures = app.MapGroup("/api/lectures").WithTags("Lectures").RequireAuthorization();

        courseLectures.MapGet("/", async Task<Results<Ok<IReadOnlyList<LectureResponse>>, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int courseId, ClaimsPrincipal user, ILectureService lectureService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            try
            {
                var result = await lectureService.ListForCourseAsync(courseId, userId.Value, IsAdmin(user), ct);
                return result is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok<IReadOnlyList<LectureResponse>>([.. result.Select(LectureResponseMapper.ToResponse)]);
            }
            catch (GroupAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        courseLectures.MapPost("/", async Task<Results<Created<LectureResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, ForbidHttpResult>>
            (int courseId, LectureRequest request, ClaimsPrincipal user, ILectureService lectureService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new LectureRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            try
            {
                var lecture = await lectureService.ScheduleAsync(courseId, request.ToDto(), userId.Value, IsAdmin(user), ct);
                return lecture is null
                    ? TypedResults.NotFound()
                    : TypedResults.Created($"/api/lectures/{lecture.Id}", LectureResponseMapper.ToResponse(lecture));
            }
            catch (GroupAuthorizationException)
            {
                return TypedResults.Forbid();
            }
            catch (ArgumentException ex)
            {
                return InvalidArgument(ex);
            }
        });

        lectures.MapGet("/mine", async Task<Results<Ok<IReadOnlyList<LectureResponse>>, UnauthorizedHttpResult, ValidationProblem>>
            (DateTime? from, DateTime? to, ClaimsPrincipal user, ILectureService lectureService, TimeProvider timeProvider, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var now = timeProvider.GetUtcNow().UtcDateTime;
            try
            {
                var result = await lectureService.ListMyScheduleAsync(
                    userId.Value, from ?? now - DefaultSchedulePast, to ?? now + DefaultScheduleFuture, ct);
                return TypedResults.Ok<IReadOnlyList<LectureResponse>>([.. result.Select(LectureResponseMapper.ToResponse)]);
            }
            catch (ArgumentException ex)
            {
                return InvalidArgument(ex);
            }
        });

        lectures.MapPut("/{id:int}", async Task<Results<Ok<LectureResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, Conflict<string>, ForbidHttpResult>>
            (int id, LectureRequest request, ClaimsPrincipal user, ILectureService lectureService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new LectureRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            try
            {
                var lecture = await lectureService.UpdateAsync(id, request.ToDto(), userId.Value, IsAdmin(user), ct);
                return lecture is null ? TypedResults.NotFound() : TypedResults.Ok(LectureResponseMapper.ToResponse(lecture));
            }
            catch (GroupAuthorizationException)
            {
                return TypedResults.Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return InvalidArgument(ex);
            }
        });

        lectures.MapPost("/{id:int}/cancel", async Task<Results<Ok<LectureResponse>, UnauthorizedHttpResult, NotFound, Conflict<string>, ForbidHttpResult>>
            (int id, ClaimsPrincipal user, ILectureService lectureService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            try
            {
                var lecture = await lectureService.CancelAsync(id, userId.Value, IsAdmin(user), ct);
                return lecture is null ? TypedResults.NotFound() : TypedResults.Ok(LectureResponseMapper.ToResponse(lecture));
            }
            catch (GroupAuthorizationException)
            {
                return TypedResults.Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
        });

        lectures.MapDelete("/{id:int}", async Task<Results<NoContent, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int id, ClaimsPrincipal user, ILectureService lectureService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            try
            {
                return await lectureService.DeleteAsync(id, userId.Value, IsAdmin(user), ct)
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound();
            }
            catch (GroupAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });
    }

    private static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole(nameof(UserRole.Admin));

    private static ValidationProblem InvalidArgument(ArgumentException ex) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [ex.ParamName ?? "request"] = [ex.Message],
        });
}
