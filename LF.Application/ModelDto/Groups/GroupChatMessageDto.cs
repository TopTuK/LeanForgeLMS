namespace LF.Application.ModelDto.Groups;

public sealed class GroupChatMessageDto
{
    public int Id { get; init; }
    public int GroupId { get; init; }
    public int AuthorUserId { get; init; }
    public string AuthorName { get; init; } = null!;

    // Lets the UI badge replies from the course creator / instructors.
    public bool AuthorIsStaff { get; init; }

    // Null once deleted: the row stays so the conversation keeps its shape.
    public string? Body { get; init; }
    public DateTime SentAt { get; init; }
    public bool IsDeleted { get; init; }

    // Relative to the viewer. Always false on real-time pushes, which go to every member at once;
    // clients compare AuthorUserId with GroupChatPageDto.ViewerUserId there.
    public bool IsMine { get; init; }
}
