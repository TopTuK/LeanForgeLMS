using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Groups;
using LF.WebApi.Endpoints;
using Microsoft.AspNetCore.SignalR;

namespace LF.WebApi.Hubs;

// In-process fan-out: correct as long as LF.WebApi runs as a single replica. Scaling out would need a
// backplane (e.g. Redis) plus a shared subscriber registry.
internal sealed class SignalRGroupChatNotifier(
    IHubContext<GroupChatHub> hubContext,
    GroupChatConnectionRegistry registry) : IGroupChatNotifier
{
    private readonly IHubContext<GroupChatHub> _hubContext = hubContext;
    private readonly GroupChatConnectionRegistry _registry = registry;

    public Task MessagePostedAsync(GroupChatMessageDto message, IReadOnlyCollection<int> recipientUserIds, CancellationToken cancellationToken = default) =>
        SendAsync(message.GroupId, recipientUserIds, GroupChatHub.MessagePostedEvent, GroupChatResponseMapper.ToResponse(message), cancellationToken);

    public Task MessageDeletedAsync(int groupId, int messageId, IReadOnlyCollection<int> recipientUserIds, CancellationToken cancellationToken = default) =>
        SendAsync(groupId, recipientUserIds, GroupChatHub.MessageDeletedEvent, new GroupChatMessageDeletedEvent(groupId, messageId), cancellationToken);

    private Task SendAsync(int groupId, IReadOnlyCollection<int> recipientUserIds, string eventName, object payload, CancellationToken cancellationToken)
    {
        var connectionIds = new List<string>();
        foreach (var (connectionId, subscriber) in _registry.Get(groupId))
        {
            if (subscriber.IsAdmin || recipientUserIds.Contains(subscriber.UserId))
                connectionIds.Add(connectionId);
            else
                _registry.Remove(groupId, connectionId);
        }

        return connectionIds.Count == 0
            ? Task.CompletedTask
            : _hubContext.Clients.Clients(connectionIds).SendAsync(eventName, payload, cancellationToken);
    }
}
