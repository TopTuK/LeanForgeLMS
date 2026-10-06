using LF.Application.ModelDto.Groups;

namespace LF.Application.Common.Interfaces;

// Pushes chat events to connected clients. Implemented in LF.WebApi over SignalR so the Application
// layer stays free of ASP.NET Core types. Called after the change is committed.
//
// recipientUserIds is who may read the group right now (staff + members with an Active enrollment).
// A connection subscribed earlier by anyone else has since lost access and must not receive the event.
public interface IGroupChatNotifier
{
    Task MessagePostedAsync(GroupChatMessageDto message, IReadOnlyCollection<int> recipientUserIds, CancellationToken cancellationToken = default);

    Task MessageDeletedAsync(int groupId, int messageId, IReadOnlyCollection<int> recipientUserIds, CancellationToken cancellationToken = default);
}
