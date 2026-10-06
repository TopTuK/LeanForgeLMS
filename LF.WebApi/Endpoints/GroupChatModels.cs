using FluentValidation;
using LF.AppDomain.Entities.Groups;
using LF.Application.ModelDto.Groups;

namespace LF.WebApi.Endpoints;

public sealed record PostGroupChatMessageRequest(string Body);

public sealed class PostGroupChatMessageRequestValidator : AbstractValidator<PostGroupChatMessageRequest>
{
    public PostGroupChatMessageRequestValidator()
    {
        RuleFor(x => x.Body).NotEmpty().Must(b => !string.IsNullOrWhiteSpace(b)).MaximumLength(GroupChatMessage.MaxBodyLength);
    }
}

public sealed record MarkGroupChatReadRequest(int LastSeenMessageId);

public sealed record GroupChatMessageResponse(
    int Id,
    int GroupId,
    int AuthorUserId,
    string AuthorName,
    bool AuthorIsStaff,
    string? Body,
    DateTime SentAt,
    bool IsDeleted,
    bool IsMine);

public sealed record GroupChatPageResponse(IReadOnlyList<GroupChatMessageResponse> Items, bool HasMore, int ViewerUserId);

public sealed record GroupUnreadCountResponse(int GroupId, int Count);

// Payload of the hub's "messageDeleted" event.
public sealed record GroupChatMessageDeletedEvent(int GroupId, int MessageId);

public static class GroupChatResponseMapper
{
    public static GroupChatMessageResponse ToResponse(GroupChatMessageDto dto) =>
        new(dto.Id, dto.GroupId, dto.AuthorUserId, dto.AuthorName, dto.AuthorIsStaff, dto.Body, dto.SentAt, dto.IsDeleted, dto.IsMine);

    public static GroupChatPageResponse ToResponse(GroupChatPageDto dto) =>
        new([.. dto.Items.Select(ToResponse)], dto.HasMore, dto.ViewerUserId);
}
