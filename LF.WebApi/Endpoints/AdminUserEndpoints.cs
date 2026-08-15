using System.Security.Claims;
using FluentValidation;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.ModelDto.User;
using LF.Application.Services.Admin;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

public sealed class AdminUserEndpoints : IEndpointGroup
{
    private const int DefaultPageSize = 20;

    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users").WithTags("AdminUsers").RequireAuthorization("AdminOnly");

        group.MapGet("/", async Task<Results<Ok<PagedAdminUserResponse>, UnauthorizedHttpResult>>
            (int? page, int? pageSize, string? search, ClaimsPrincipal user, IAdminUserService adminUserService, CancellationToken ct) =>
        {
            if (user.GetUserId() is null) return TypedResults.Unauthorized();

            var effectivePage = page is > 0 ? page.Value : 1;
            var effectivePageSize = pageSize is > 0 ? pageSize.Value : DefaultPageSize;

            var result = await adminUserService.ListUsersAsync(effectivePage, effectivePageSize, search);
            return TypedResults.Ok(new PagedAdminUserResponse(
                [.. result.Items.Select(ToResponse)],
                result.TotalCount,
                effectivePage,
                effectivePageSize));
        });

        group.MapPut("/{id:int}", async Task<Results<Ok<AdminUserResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem>>
            (int id, UpdateAdminUserInfoRequest request, ClaimsPrincipal user, IAdminUserService adminUserService, CancellationToken ct) =>
        {
            if (user.GetUserId() is null) return TypedResults.Unauthorized();

            var validation = new UpdateAdminUserInfoRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            var updated = await adminUserService.UpdateUserInfoAsync(id, new UpdateUserProfileDto { FirstName = request.FirstName, LastName = request.LastName, Description = request.Description });
            return updated is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(updated));
        });

        group.MapPut("/{id:int}/role", async Task<Results<Ok<AdminUserResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, Conflict<string>>>
            (int id, UpdateAdminUserRoleRequest request, ClaimsPrincipal user, IAdminUserService adminUserService, CancellationToken ct) =>
        {
            var adminId = user.GetUserId();
            if (adminId is null) return TypedResults.Unauthorized();

            if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role) || role == UserRole.None)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["role"] = ["Role must be one of Student, Instructor, CourseCreator, Admin."],
                });
            }

            try
            {
                var updated = await adminUserService.UpdateUserRoleAsync(id, adminId.Value, role);
                return updated is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(updated));
            }
            catch (SelfAdministrationException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
        });

        group.MapDelete("/{id:int}", async Task<Results<NoContent, UnauthorizedHttpResult, NotFound, Conflict<string>>>
            (int id, ClaimsPrincipal user, IAdminUserService adminUserService, CancellationToken ct) =>
        {
            var adminId = user.GetUserId();
            if (adminId is null) return TypedResults.Unauthorized();

            try
            {
                var deleted = await adminUserService.DeleteUserAsync(id, adminId.Value);
                return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
            }
            catch (SelfAdministrationException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
        });
    }

    private static AdminUserResponse ToResponse(UserDto user) =>
        new(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString(), user.CreatedAt, user.Description);
}
