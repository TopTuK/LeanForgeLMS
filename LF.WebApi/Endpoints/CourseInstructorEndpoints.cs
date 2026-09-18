using System.Security.Claims;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.ModelDto.Qna;
using LF.Application.Services.CourseTeaching;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

// Who may answer a course's questions. Kept out of CourseEndpoints because that group routes
// everything through IGrpcCourseService, while the teaching team is stored and read locally.
public sealed class CourseInstructorEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/courses/{courseId:int}/instructors")
            .WithTags("CourseInstructors")
            .RequireAuthorization("CourseCreatorOrAdmin");

        group.MapGet("/", async Task<Results<Ok<IReadOnlyList<CourseInstructorResponse>>, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int courseId, ClaimsPrincipal user, ICourseTeachingTeamService teamService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            try
            {
                var team = await teamService.ListAsync(courseId, userId.Value, user.IsInRole(nameof(UserRole.Admin)), ct);
                return team is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(team));
            }
            catch (QuestionAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapPost("/", async Task<Results<Ok<IReadOnlyList<CourseInstructorResponse>>, UnauthorizedHttpResult, NotFound, ValidationProblem, Conflict<string>, ForbidHttpResult>>
            (int courseId, AssignCourseInstructorRequest request, ClaimsPrincipal user, ICourseTeachingTeamService teamService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new AssignCourseInstructorRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            try
            {
                var team = await teamService.AssignAsync(courseId, request.Email, userId.Value, user.IsInRole(nameof(UserRole.Admin)), ct);
                return team is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(team));
            }
            catch (QuestionAuthorizationException)
            {
                return TypedResults.Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
        });

        group.MapDelete("/{userId:int}", async Task<Results<Ok<IReadOnlyList<CourseInstructorResponse>>, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int courseId, int userId, ClaimsPrincipal user, ICourseTeachingTeamService teamService, CancellationToken ct) =>
        {
            var actingUserId = user.GetUserId();
            if (actingUserId is null) return TypedResults.Unauthorized();

            try
            {
                var team = await teamService.RemoveAsync(courseId, userId, actingUserId.Value, user.IsInRole(nameof(UserRole.Admin)), ct);
                return team is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(team));
            }
            catch (QuestionAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });
    }

    private static IReadOnlyList<CourseInstructorResponse> ToResponse(IReadOnlyList<CourseInstructorDto> team) =>
        [.. team.Select(CourseInstructorResponseMapper.ToResponse)];
}
