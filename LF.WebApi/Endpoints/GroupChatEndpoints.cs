using System.Security.Claims;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.Services.Groups;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

// Group chat over REST: history, sending, deleting and read markers. Real-time delivery of what is
// posted here goes out through GroupChatHub.
public sealed class GroupChatEndpoints : IEndpointGroup
{
    private const int DefaultPageSize = 30;

    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/groups").WithTags("GroupChat").RequireAuthorization();

        group.MapGet("/{id:int}/messages", async Task<Results<Ok<GroupChatPageResponse>, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int id, int? beforeId, int? take, ClaimsPrincipal user, IGroupChatService chatService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            try
            {
                var page = await chatService.GetHistoryAsync(id, beforeId, take ?? DefaultPageSize, userId.Value, IsAdmin(user), ct);
                return page is null ? TypedResults.NotFound() : TypedResults.Ok(GroupChatResponseMapper.ToResponse(page));
            }
            catch (GroupAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapPost("/{id:int}/messages", async Task<Results<Created<GroupChatMessageResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, ForbidHttpResult>>
            (int id, PostGroupChatMessageRequest request, ClaimsPrincipal user, IGroupChatService chatService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new PostGroupChatMessageRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            try
            {
                var message = await chatService.PostAsync(id, request.Body, userId.Value, IsAdmin(user), ct);
                return message is null
                    ? TypedResults.NotFound()
                    : TypedResults.Created($"/api/groups/{id}/messages/{message.Id}", GroupChatResponseMapper.ToResponse(message));
            }
            catch (GroupAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapDelete("/{id:int}/messages/{messageId:int}", async Task<Results<Ok<GroupChatMessageResponse>, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int id, int messageId, ClaimsPrincipal user, IGroupChatService chatService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            try
            {
                var message = await chatService.DeleteMessageAsync(id, messageId, userId.Value, IsAdmin(user), ct);
                return message is null ? TypedResults.NotFound() : TypedResults.Ok(GroupChatResponseMapper.ToResponse(message));
            }
            catch (GroupAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapPost("/{id:int}/messages/read", async Task<Results<NoContent, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int id, MarkGroupChatReadRequest request, ClaimsPrincipal user, IGroupChatService chatService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            try
            {
                return await chatService.MarkReadAsync(id, request.LastSeenMessageId, userId.Value, IsAdmin(user), ct)
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound();
            }
            catch (GroupAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapGet("/unread", async Task<Results<Ok<IReadOnlyList<GroupUnreadCountResponse>>, UnauthorizedHttpResult>>
            (ClaimsPrincipal user, IGroupChatService chatService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var counts = await chatService.GetUnreadCountsAsync(userId.Value, ct);
            return TypedResults.Ok<IReadOnlyList<GroupUnreadCountResponse>>([.. counts.Select(c => new GroupUnreadCountResponse(c.GroupId, c.Count))]);
        });
    }

    private static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole(nameof(UserRole.Admin));
}
