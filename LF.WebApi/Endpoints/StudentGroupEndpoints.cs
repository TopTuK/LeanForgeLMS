using System.Security.Claims;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.Services.Groups;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

// Student groups. Like Q&A, authorization is per course (teaching staff = creator + assigned
// instructors) and lives in the service, so routes only require an authenticated user.
public sealed class StudentGroupEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var courseGroups = app.MapGroup("/api/courses/{courseId:int}/groups").WithTags("StudentGroups").RequireAuthorization();
        var groups = app.MapGroup("/api/groups").WithTags("StudentGroups").RequireAuthorization();

        courseGroups.MapGet("/", async Task<Results<Ok<IReadOnlyList<StudentGroupSummaryResponse>>, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int courseId, ClaimsPrincipal user, IStudentGroupService groupService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            try
            {
                var result = await groupService.ListForCourseAsync(courseId, userId.Value, IsAdmin(user), ct);
                return result is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok<IReadOnlyList<StudentGroupSummaryResponse>>([.. result.Select(StudentGroupResponseMapper.ToResponse)]);
            }
            catch (GroupAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        courseGroups.MapPost("/", async Task<Results<Created<StudentGroupDetailResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, Conflict<string>, ForbidHttpResult>>
            (int courseId, StudentGroupRequest request, ClaimsPrincipal user, IStudentGroupService groupService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new StudentGroupRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            try
            {
                var group = await groupService.CreateAsync(courseId, request.Name, request.Description, userId.Value, IsAdmin(user), ct);
                return group is null
                    ? TypedResults.NotFound()
                    : TypedResults.Created($"/api/groups/{group.Id}", StudentGroupResponseMapper.ToResponse(group));
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

        courseGroups.MapGet("/eligible-students", async Task<Results<Ok<IReadOnlyList<EligibleStudentResponse>>, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int courseId, ClaimsPrincipal user, IStudentGroupService groupService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            try
            {
                var result = await groupService.ListEligibleStudentsAsync(courseId, userId.Value, IsAdmin(user), ct);
                return result is null
                    ? TypedResults.NotFound()
                    : TypedResults.Ok<IReadOnlyList<EligibleStudentResponse>>([.. result.Select(StudentGroupResponseMapper.ToResponse)]);
            }
            catch (GroupAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        groups.MapGet("/mine", async Task<Results<Ok<IReadOnlyList<StudentGroupSummaryResponse>>, UnauthorizedHttpResult>>
            (ClaimsPrincipal user, IStudentGroupService groupService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var result = await groupService.ListMyGroupsAsync(userId.Value, ct);
            return TypedResults.Ok<IReadOnlyList<StudentGroupSummaryResponse>>([.. result.Select(StudentGroupResponseMapper.ToResponse)]);
        });

        groups.MapGet("/{id:int}", async Task<Results<Ok<StudentGroupDetailResponse>, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int id, ClaimsPrincipal user, IStudentGroupService groupService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            try
            {
                var group = await groupService.GetAsync(id, userId.Value, IsAdmin(user), ct);
                return group is null ? TypedResults.NotFound() : TypedResults.Ok(StudentGroupResponseMapper.ToResponse(group));
            }
            catch (GroupAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        groups.MapPut("/{id:int}", async Task<Results<Ok<StudentGroupDetailResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, Conflict<string>, ForbidHttpResult>>
            (int id, StudentGroupRequest request, ClaimsPrincipal user, IStudentGroupService groupService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new StudentGroupRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            try
            {
                var group = await groupService.UpdateAsync(id, request.Name, request.Description, userId.Value, IsAdmin(user), ct);
                return group is null ? TypedResults.NotFound() : TypedResults.Ok(StudentGroupResponseMapper.ToResponse(group));
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

        groups.MapDelete("/{id:int}", async Task<Results<NoContent, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int id, ClaimsPrincipal user, IStudentGroupService groupService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            try
            {
                return await groupService.DeleteAsync(id, userId.Value, IsAdmin(user), ct)
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound();
            }
            catch (GroupAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        groups.MapPost("/{id:int}/members", async Task<Results<Ok<StudentGroupDetailResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, Conflict<string>, ForbidHttpResult>>
            (int id, AddGroupMembersRequest request, ClaimsPrincipal user, IStudentGroupService groupService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new AddGroupMembersRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            try
            {
                var group = await groupService.AddMembersAsync(id, request.UserIds, userId.Value, IsAdmin(user), ct);
                return group is null ? TypedResults.NotFound() : TypedResults.Ok(StudentGroupResponseMapper.ToResponse(group));
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

        groups.MapDelete("/{id:int}/members/{memberUserId:int}", async Task<Results<Ok<StudentGroupDetailResponse>, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int id, int memberUserId, ClaimsPrincipal user, IStudentGroupService groupService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            try
            {
                var group = await groupService.RemoveMemberAsync(id, memberUserId, userId.Value, IsAdmin(user), ct);
                return group is null ? TypedResults.NotFound() : TypedResults.Ok(StudentGroupResponseMapper.ToResponse(group));
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
