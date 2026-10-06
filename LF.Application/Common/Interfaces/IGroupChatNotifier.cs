using LF.Application.ModelDto.Groups;

namespace LF.Application.Common.Interfaces;

// Pushes chat events to connected clients. Implemented in LF.WebApi over SignalR so the Application
// layer stays free of ASP.NET Core types. Called after the change is committed.
public interface IGroupChatNotifier
{
    Task MessagePostedAsync(GroupChatMessageDto message, CancellationToken cancellationToken = default);

    Task MessageDeletedAsync(int groupId, int messageId, CancellationToken cancellationToken = default);
}
