namespace LF.Application.ModelDto.Groups;

public sealed class GroupChatPageDto
{
    // Oldest first, so clients can render the page as-is.
    public IReadOnlyList<GroupChatMessageDto> Items { get; init; } = [];
    public bool HasMore { get; init; }

    // Real-time pushes are viewer-neutral (IsMine is always false there), and the SPA has no user id
    // of its own, so the chat view takes it from here to recognise its own messages.
    public int ViewerUserId { get; init; }
}
