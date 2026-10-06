using LF.AppDomain.Models.User.Enums;
using LF.Application.Services.Groups;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LF.WebApi.Hubs;

// Push-only: clients subscribe to the groups they have open and receive "messagePosted" /
// "messageDeleted". Messages are sent over REST (GroupChatEndpoints) so validation, authorization
// and error mapping live in one place. Authenticated by the same HttpOnly session cookie as the API,
// which the browser sends on the same-origin WebSocket handshake.
[Authorize]
internal sealed class GroupChatHub(IGroupChatService chatService) : Hub
{
    public const string Path = "/hubs/group-chat";
    public const string MessagePostedEvent = "messagePosted";
    public const string MessageDeletedEvent = "messageDeleted";

    private readonly IGroupChatService _chatService = chatService;

    public static string GroupKey(int groupId) => $"group:{groupId}";

    public async Task JoinGroup(int groupId)
    {
        var userId = Context.User?.GetUserId() ?? throw new HubException("Not authenticated.");
        var isAdmin = Context.User.IsInRole(nameof(UserRole.Admin));

        if (!await _chatService.CanAccessAsync(groupId, userId, isAdmin, Context.ConnectionAborted))
            throw new HubException("You do not have access to this group's chat.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupKey(groupId), Context.ConnectionAborted);
    }

    public Task LeaveGroup(int groupId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupKey(groupId), Context.ConnectionAborted);
}
