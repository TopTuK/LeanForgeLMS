using System.Security.Claims;
using LF.AppDomain.Models.Qna.Enums;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.ModelDto.Qna;
using LF.Application.Services.Qna;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

// The student and teaching-staff Q&A surface. Authorization is per-course and lives in the service:
// the global Instructor role says nothing about any particular course, so a route policy can't
// express it. Endpoints just pass down actingUserId + isAdmin and map the exceptions back.
public sealed class LessonQuestionEndpoints : IEndpointGroup
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/questions").WithTags("Questions").RequireAuthorization();

        group.MapGet("/", async Task<Results<Ok<PagedLessonQuestionsResponse>, UnauthorizedHttpResult, ValidationProblem>>
            (string? scope, int? courseId, string? status, string? search, int? page, int? pageSize,
             ClaimsPrincipal user, ILessonQuestionService questionService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            if (search?.Length > LessonQuestionSearch.MaxLength)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["search"] = [$"Search cannot exceed {LessonQuestionSearch.MaxLength} characters."],
                });
            }

            LessonQuestionStatus? statusFilter = null;
            if (status is not null)
            {
                if (!LessonQuestionResponseMapper.TryParseStatus(status, out var parsed)) return InvalidStatus();
                statusFilter = parsed;
            }

            var effectivePage = page is > 0 ? page.Value : 1;
            var effectivePageSize = pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;

            var result = await questionService.ListAsync(
                userId.Value,
                LessonQuestionResponseMapper.ParseScope(scope),
                courseId,
                statusFilter,
                search,
                effectivePage,
                effectivePageSize,
                ct);

            return TypedResults.Ok(ToPagedResponse(result, effectivePage, effectivePageSize));
        });

        // Powers the Q&A panel on the lesson page: the caller's own threads as a student, or every
        // thread on that lesson when they teach the course.
        app.MapGet("/api/lessons/{lessonId:int}/questions", async Task<Results<Ok<PagedLessonQuestionsResponse>, UnauthorizedHttpResult>>
            (int lessonId, int? page, int? pageSize, ClaimsPrincipal user, ILessonQuestionService questionService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var effectivePage = page is > 0 ? page.Value : 1;
            var effectivePageSize = pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;

            var result = await questionService.ListForLessonAsync(
                lessonId, userId.Value, user.IsInRole(nameof(UserRole.Admin)), effectivePage, effectivePageSize, ct);

            return TypedResults.Ok(ToPagedResponse(result, effectivePage, effectivePageSize));
        }).WithTags("Questions").RequireAuthorization();

        group.MapPost("/", async Task<Results<Created<LessonQuestionThreadResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, ForbidHttpResult>>
            (AskQuestionRequest request, ClaimsPrincipal user, ILessonQuestionService questionService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new AskQuestionRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            try
            {
                var thread = await questionService.AskAsync(
                    new AskQuestionDto { LessonId = request.LessonId, Title = request.Title, Body = request.Body },
                    userId.Value,
                    ct);

                return thread is null
                    ? TypedResults.NotFound()
                    : TypedResults.Created($"/api/questions/{thread.Id}", LessonQuestionResponseMapper.ToResponse(thread));
            }
            catch (QuestionAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapGet("/{id:int}", async Task<Results<Ok<LessonQuestionThreadResponse>, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int id, ClaimsPrincipal user, ILessonQuestionService questionService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            try
            {
                var thread = await questionService.GetThreadAsync(id, userId.Value, user.IsInRole(nameof(UserRole.Admin)), ct);
                return thread is null ? TypedResults.NotFound() : TypedResults.Ok(LessonQuestionResponseMapper.ToResponse(thread));
            }
            catch (QuestionAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapPost("/{id:int}/messages", async Task<Results<Ok<LessonQuestionThreadResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, Conflict<string>, ForbidHttpResult>>
            (int id, PostQuestionMessageRequest request, ClaimsPrincipal user, ILessonQuestionService questionService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new PostQuestionMessageRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            try
            {
                var thread = await questionService.PostMessageAsync(
                    id, new PostQuestionMessageDto { Body = request.Body }, userId.Value, user.IsInRole(nameof(UserRole.Admin)), ct);

                return thread is null ? TypedResults.NotFound() : TypedResults.Ok(LessonQuestionResponseMapper.ToResponse(thread));
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

        group.MapPost("/{id:int}/close", (int id, ClaimsPrincipal user, ILessonQuestionService questionService, CancellationToken ct) =>
            SetStatusAsync(id, close: true, user, questionService, ct));

        group.MapPost("/{id:int}/reopen", (int id, ClaimsPrincipal user, ILessonQuestionService questionService, CancellationToken ct) =>
            SetStatusAsync(id, close: false, user, questionService, ct));

        group.MapGet("/overview", async Task<Results<Ok<LessonQuestionOverviewResponse>, UnauthorizedHttpResult>>
            (string? scope, ClaimsPrincipal user, ILessonQuestionService questionService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var overview = await questionService.GetOverviewAsync(userId.Value, LessonQuestionResponseMapper.ParseScope(scope), ct);
            return TypedResults.Ok(LessonQuestionResponseMapper.ToResponse(overview));
        });

        group.MapGet("/unread-count", async Task<Results<Ok<QuestionUnreadCountResponse>, UnauthorizedHttpResult>>
            (ClaimsPrincipal user, ILessonQuestionService questionService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var count = await questionService.GetUnreadCountAsync(userId.Value, ct);
            return TypedResults.Ok(new QuestionUnreadCountResponse(count));
        });
    }

    private static async Task<Results<Ok<LessonQuestionThreadResponse>, UnauthorizedHttpResult, NotFound, Conflict<string>, ForbidHttpResult>> SetStatusAsync(
        int id,
        bool close,
        ClaimsPrincipal user,
        ILessonQuestionService questionService,
        CancellationToken ct)
    {
        var userId = user.GetUserId();
        if (userId is null) return TypedResults.Unauthorized();

        try
        {
            var thread = await questionService.SetStatusAsync(id, close, userId.Value, user.IsInRole(nameof(UserRole.Admin)), ct);
            return thread is null ? TypedResults.NotFound() : TypedResults.Ok(LessonQuestionResponseMapper.ToResponse(thread));
        }
        catch (QuestionAuthorizationException)
        {
            return TypedResults.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(ex.Message);
        }
    }

    private static PagedLessonQuestionsResponse ToPagedResponse(PagedLessonQuestionsDto result, int page, int pageSize) =>
        new([.. result.Items.Select(LessonQuestionResponseMapper.ToResponse)], result.TotalCount, page, pageSize);

    private static ValidationProblem InvalidStatus() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            ["status"] = ["Status must be one of Open, Answered, Closed."],
        });
}
