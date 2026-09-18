using System.Security.Claims;
using LF.AppDomain.Models.Qna.Enums;
using LF.Application.ModelDto.Qna;
using LF.Application.Services.Admin;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

// Platform-wide Q&A oversight. Unlike the student/staff group this one is gated purely by the
// AdminOnly policy — an admin sees every thread on every course.
public sealed class AdminLessonQuestionEndpoints : IEndpointGroup
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/questions").WithTags("AdminQuestions").RequireAuthorization("AdminOnly");

        group.MapGet("/", async Task<Results<Ok<PagedLessonQuestionsResponse>, UnauthorizedHttpResult, ValidationProblem>>
            (int? courseId, string? status, string? search, int? page, int? pageSize,
             ClaimsPrincipal user, IAdminLessonQuestionService questionService, CancellationToken ct) =>
        {
            var adminId = user.GetUserId();
            if (adminId is null) return TypedResults.Unauthorized();

            LessonQuestionStatus? statusFilter = null;
            if (status is not null)
            {
                if (!LessonQuestionResponseMapper.TryParseStatus(status, out var parsed))
                {
                    return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["status"] = ["Status must be one of Open, Answered, Closed."],
                    });
                }

                statusFilter = parsed;
            }

            var effectivePage = page is > 0 ? page.Value : 1;
            var effectivePageSize = pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;

            var result = await questionService.ListAsync(courseId, statusFilter, search, adminId.Value, effectivePage, effectivePageSize, ct);
            return TypedResults.Ok(new PagedLessonQuestionsResponse(
                [.. result.Items.Select(LessonQuestionResponseMapper.ToResponse)], result.TotalCount, effectivePage, effectivePageSize));
        });

        group.MapGet("/{id:int}", async Task<Results<Ok<LessonQuestionThreadResponse>, UnauthorizedHttpResult, NotFound>>
            (int id, ClaimsPrincipal user, IAdminLessonQuestionService questionService, CancellationToken ct) =>
        {
            var adminId = user.GetUserId();
            if (adminId is null) return TypedResults.Unauthorized();

            var thread = await questionService.GetThreadAsync(id, adminId.Value, ct);
            return thread is null ? TypedResults.NotFound() : TypedResults.Ok(LessonQuestionResponseMapper.ToResponse(thread));
        });

        group.MapPost("/{id:int}/messages", async Task<Results<Ok<LessonQuestionThreadResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, Conflict<string>>>
            (int id, PostQuestionMessageRequest request, ClaimsPrincipal user, IAdminLessonQuestionService questionService, CancellationToken ct) =>
        {
            var adminId = user.GetUserId();
            if (adminId is null) return TypedResults.Unauthorized();

            var validation = new PostQuestionMessageRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            try
            {
                var thread = await questionService.PostMessageAsync(id, new PostQuestionMessageDto { Body = request.Body }, adminId.Value, ct);
                return thread is null ? TypedResults.NotFound() : TypedResults.Ok(LessonQuestionResponseMapper.ToResponse(thread));
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
        });

        group.MapDelete("/{id:int}/messages/{messageId:int}", async Task<Results<Ok<LessonQuestionThreadResponse>, UnauthorizedHttpResult, NotFound>>
            (int id, int messageId, ClaimsPrincipal user, IAdminLessonQuestionService questionService, CancellationToken ct) =>
        {
            var adminId = user.GetUserId();
            if (adminId is null) return TypedResults.Unauthorized();

            var thread = await questionService.DeleteMessageAsync(id, messageId, adminId.Value, ct);
            return thread is null ? TypedResults.NotFound() : TypedResults.Ok(LessonQuestionResponseMapper.ToResponse(thread));
        });

        group.MapDelete("/{id:int}", async Task<Results<NoContent, UnauthorizedHttpResult, NotFound>>
            (int id, ClaimsPrincipal user, IAdminLessonQuestionService questionService, CancellationToken ct) =>
        {
            if (user.GetUserId() is null) return TypedResults.Unauthorized();

            var deleted = await questionService.DeleteThreadAsync(id, ct);
            return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
        });
    }
}
