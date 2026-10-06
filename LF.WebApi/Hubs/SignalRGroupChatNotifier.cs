using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Groups;
using LF.WebApi.Endpoints;
using Microsoft.AspNetCore.SignalR;

namespace LF.WebApi.Hubs;

// In-process SignalR groups: correct as long as LF.WebApi runs as a single replica. Scaling out would
// need a backplane (e.g. Redis) so a message posted on one instance reaches sockets on another.
internal sealed class SignalRGroupChatNotifier(IHubContext<GroupChatHub> hubContext) : IGroupChatNotifier
{
    private readonly IHubContext<GroupChatHub> _hubContext = hubContext;

    public Task MessagePostedAsync(GroupChatMessageDto message, CancellationToken cancellationToken = default) =>
        _hubContext.Clients
            .Group(GroupChatHub.GroupKey(message.GroupId))
            .SendAsync(GroupChatHub.MessagePostedEvent, GroupChatResponseMapper.ToResponse(message), cancellationToken);

    public Task MessageDeletedAsync(int groupId, int messageId, CancellationToken cancellationToken = default) =>
        _hubContext.Clients
            .Group(GroupChatHub.GroupKey(groupId))
            .SendAsync(GroupChatHub.MessageDeletedEvent, new GroupChatMessageDeletedEvent(groupId, messageId), cancellationToken);
}
