using System.Security.Claims;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Services.Teaching;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

// Read locally rather than through IGrpcCourseService: the gRPC ListCourses is owner-scoped and has
// no notion of assigned instructors.
public sealed class TeachingEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teaching").WithTags("Teaching").RequireAuthorization("CourseCreatorOrAdmin");

        group.MapGet("/courses", async Task<Results<Ok<IReadOnlyList<TeachingCourseResponse>>, UnauthorizedHttpResult>>
            (ClaimsPrincipal user, ITeachingCourseService teachingService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var courses = await teachingService.ListMyTeachingCoursesAsync(userId.Value, user.IsInRole(nameof(UserRole.Admin)), ct);
            return TypedResults.Ok<IReadOnlyList<TeachingCourseResponse>>([.. courses.Select(TeachingCourseResponseMapper.ToResponse)]);
        });
    }
}
