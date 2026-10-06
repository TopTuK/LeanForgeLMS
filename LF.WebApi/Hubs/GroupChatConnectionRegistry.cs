using System.Collections.Concurrent;

namespace LF.WebApi.Hubs;

internal sealed record GroupChatSubscriber(int UserId, bool IsAdmin);

// Which connection joined which group as whom. Access is checked once at JoinGroup, but it can be lost
// later (removed from the group, enrollment removed), so every push re-filters subscribers against the
// current recipient list instead of trusting SignalR group membership. In-process, so like the hub it
// assumes a single LF.WebApi replica.
internal sealed class GroupChatConnectionRegistry
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, GroupChatSubscriber>> _groups = new();

    public void Add(int groupId, string connectionId, GroupChatSubscriber subscriber) =>
        _groups.GetOrAdd(groupId, _ => new ConcurrentDictionary<string, GroupChatSubscriber>())[connectionId] = subscriber;

    public void Remove(int groupId, string connectionId)
    {
        if (_groups.TryGetValue(groupId, out var connections))
            connections.TryRemove(connectionId, out _);
    }

    public void RemoveConnection(string connectionId)
    {
        foreach (var connections in _groups.Values)
            connections.TryRemove(connectionId, out _);
    }

    public IReadOnlyList<KeyValuePair<string, GroupChatSubscriber>> Get(int groupId) =>
        _groups.TryGetValue(groupId, out var connections) ? [.. connections] : [];
}
