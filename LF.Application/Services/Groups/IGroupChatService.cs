using LF.Application.ModelDto.Groups;

namespace LF.Application.Services.Groups;

// One chat room per student group, shared by its members and the course's teaching staff. Messages are
// written over REST through this service; real-time delivery is IGroupChatNotifier's job.
public interface IGroupChatService
{
    public const int MaxPageSize = 50;

    Task<GroupChatPageDto?> GetHistoryAsync(int groupId, int? beforeMessageId, int take, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<GroupChatMessageDto?> PostAsync(int groupId, string body, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<GroupChatMessageDto?> DeleteMessageAsync(int groupId, int messageId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    // Returns false when the group doesn't exist.
    Task<bool> MarkReadAsync(int groupId, int lastSeenMessageId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);

    // Unread counts for every group the user can chat in; groups with nothing unread are omitted.
    Task<IReadOnlyList<GroupUnreadCountDto>> GetUnreadCountsAsync(int actingUserId, CancellationToken cancellationToken = default);

    // The real-time hub's gate before subscribing a connection to a group's events.
    Task<bool> CanAccessAsync(int groupId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default);
}
