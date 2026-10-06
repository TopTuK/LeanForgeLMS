using LF.AppDomain.Entities.Groups;
using LF.AppDomain.Models.Course.Enums;
using LF.Application.Common.Access;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Groups;

internal sealed class GroupChatService(
    ILogger<GroupChatService> logger,
    IAppDbContext dbContext,
    IGroupChatNotifier notifier,
    TimeProvider timeProvider) : IGroupChatService
{
    private const string DeletedUserName = "—";

    private readonly ILogger<GroupChatService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly IGroupChatNotifier _notifier = notifier;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<GroupChatPageDto?> GetHistoryAsync(int groupId, int? beforeMessageId, int take, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GroupChatService::GetHistoryAsync: called with GroupId={GroupId} BeforeMessageId={BeforeMessageId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            groupId, beforeMessageId, actingUserId, isAdmin);

        var courseId = await GetGroupCourseIdAsync(groupId, cancellationToken);
        if (courseId is null)
            return null;

        await EnsureAccessAsync(groupId, courseId.Value, actingUserId, isAdmin, cancellationToken);

        var pageSize = Math.Clamp(take, 1, IGroupChatService.MaxPageSize);
        var query = _dbContext.GroupChatMessages.AsNoTracking().Where(m => m.GroupId == groupId);
        if (beforeMessageId is not null)
            query = query.Where(m => m.Id < beforeMessageId.Value);

        // Newest page first (one extra row tells us whether there's more), then flipped so the
        // client gets it in reading order.
        var page = await query
            .OrderByDescending(m => m.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = page.Count > pageSize;
        var messages = page.Take(pageSize).Reverse().ToList();

        return new GroupChatPageDto
        {
            Items = await ToDtosAsync(messages, courseId.Value, actingUserId, cancellationToken),
            HasMore = hasMore,
            ViewerUserId = actingUserId,
        };
    }

    public async Task<GroupChatMessageDto?> PostAsync(int groupId, string body, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GroupChatService::PostAsync: called with GroupId={GroupId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            groupId, actingUserId, isAdmin);

        var courseId = await GetGroupCourseIdAsync(groupId, cancellationToken);
        if (courseId is null)
            return null;

        await EnsureAccessAsync(groupId, courseId.Value, actingUserId, isAdmin, cancellationToken);

        var message = GroupChatMessage.Post(groupId, actingUserId, body, _timeProvider.GetUtcNow().UtcDateTime);
        _dbContext.GroupChatMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = (await ToDtosAsync([message], courseId.Value, actingUserId, cancellationToken))[0];

        // Broadcast copy is viewer-neutral: it goes to every subscriber, the author's other tabs included.
        var broadcast = CopyWithIsMine(dto, isMine: false);
        await PushAfterCommitAsync(groupId, courseId.Value, broadcast.Id,
            (recipients, ct) => _notifier.MessagePostedAsync(broadcast, recipients, ct));

        return dto;
    }

    public async Task<GroupChatMessageDto?> DeleteMessageAsync(int groupId, int messageId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GroupChatService::DeleteMessageAsync: called with GroupId={GroupId} MessageId={MessageId} ActingUserId={ActingUserId} IsAdmin={IsAdmin}",
            groupId, messageId, actingUserId, isAdmin);

        var courseId = await GetGroupCourseIdAsync(groupId, cancellationToken);
        if (courseId is null)
            return null;

        await EnsureAccessAsync(groupId, courseId.Value, actingUserId, isAdmin, cancellationToken);

        var message = await _dbContext.GroupChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.GroupId == groupId, cancellationToken);

        if (message is null)
            return null;

        // Students may retract their own messages; staff moderate the whole room.
        if (message.AuthorUserId != actingUserId
            && !isAdmin
            && !await _dbContext.IsTeachingStaffAsync(courseId.Value, actingUserId, cancellationToken))
        {
            throw new GroupAuthorizationException("You can only delete your own messages.");
        }

        if (message.SoftDelete(actingUserId, _timeProvider.GetUtcNow().UtcDateTime))
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await PushAfterCommitAsync(groupId, courseId.Value, messageId,
                (recipients, ct) => _notifier.MessageDeletedAsync(groupId, messageId, recipients, ct));
        }

        return (await ToDtosAsync([message], courseId.Value, actingUserId, cancellationToken))[0];
    }

    public async Task<bool> MarkReadAsync(int groupId, int lastSeenMessageId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GroupChatService::MarkReadAsync: called with GroupId={GroupId} LastSeenMessageId={LastSeenMessageId} ActingUserId={ActingUserId}",
            groupId, lastSeenMessageId, actingUserId);

        var courseId = await GetGroupCourseIdAsync(groupId, cancellationToken);
        if (courseId is null)
            return false;

        await EnsureAccessAsync(groupId, courseId.Value, actingUserId, isAdmin, cancellationToken);

        // Clamped to the newest real message so a bogus id can't pre-mark future messages as read.
        var newestId = await _dbContext.GroupChatMessages
            .AsNoTracking()
            .Where(m => m.GroupId == groupId)
            .MaxAsync(m => (int?)m.Id, cancellationToken) ?? 0;

        var seenId = Math.Min(Math.Max(lastSeenMessageId, 0), newestId);

        var marker = await _dbContext.GroupChatReadMarkers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == actingUserId, cancellationToken);

        if (marker is null)
            _dbContext.GroupChatReadMarkers.Add(GroupChatReadMarker.Create(groupId, actingUserId, seenId));
        else if (!marker.MarkSeen(seenId))
            return true;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Two tabs racing to insert the first marker: the loser's row already exists, and the
            // next read call will move it forward.
            _logger.LogWarning(ex, "GroupChatService::MarkReadAsync: concurrent marker insert for group {GroupId} user {UserId}",
                groupId, actingUserId);
        }

        return true;
    }

    public async Task<IReadOnlyList<GroupUnreadCountDto>> GetUnreadCountsAsync(int actingUserId, CancellationToken cancellationToken = default)
    {
        var staffCourseIds = _dbContext.StaffCourseIds(actingUserId);
        var memberGroupIds = _dbContext.MemberGroupIds(actingUserId);
        var chatGroupIds = _dbContext.StudentGroups
            .AsNoTracking()
            .Where(g => staffCourseIds.Contains(g.CourseId) || memberGroupIds.Contains(g.Id))
            .Select(g => g.Id);

        return await _dbContext.GroupChatMessages
            .AsNoTracking()
            .Where(m => chatGroupIds.Contains(m.GroupId) && m.AuthorUserId != actingUserId && !m.IsDeleted)
            .Where(m => !_dbContext.GroupChatReadMarkers.Any(r =>
                r.GroupId == m.GroupId && r.UserId == actingUserId && r.LastSeenMessageId >= m.Id))
            .GroupBy(m => m.GroupId)
            .Select(g => new GroupUnreadCountDto { GroupId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CanAccessAsync(int groupId, int actingUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var courseId = await GetGroupCourseIdAsync(groupId, cancellationToken);
        return courseId is not null && await HasAccessAsync(groupId, courseId.Value, actingUserId, isAdmin, cancellationToken);
    }

    private async Task<IReadOnlyList<GroupChatMessageDto>> ToDtosAsync(
        IReadOnlyList<GroupChatMessage> messages,
        int courseId,
        int viewerUserId,
        CancellationToken cancellationToken)
    {
        if (messages.Count == 0)
            return [];

        var authorIds = messages.Select(m => m.AuthorUserId).Distinct().ToList();
        var authors = await _dbContext.Users
            .AsNoTracking()
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim(), cancellationToken);

        var creatorId = await _dbContext.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => c.CreatedByUserId)
            .FirstOrDefaultAsync(cancellationToken);

        var instructorIds = await _dbContext.CourseInstructors
            .AsNoTracking()
            .Where(i => i.CourseId == courseId && authorIds.Contains(i.UserId))
            .Select(i => i.UserId)
            .ToListAsync(cancellationToken);

        var staffIds = instructorIds.Append(creatorId).ToHashSet();

        return
        [
            .. messages.Select(m => new GroupChatMessageDto
            {
                Id = m.Id,
                GroupId = m.GroupId,
                AuthorUserId = m.AuthorUserId,
                AuthorName = authors.GetValueOrDefault(m.AuthorUserId, DeletedUserName),
                AuthorIsStaff = staffIds.Contains(m.AuthorUserId),
                Body = m.IsDeleted ? null : m.Body,
                SentAt = m.SentAt,
                IsDeleted = m.IsDeleted,
                IsMine = m.AuthorUserId == viewerUserId,
            }),
        ];
    }

    // The change is already committed, so a failed push must not turn a successful request into an
    // error (the client would retry and duplicate the message); readers catch up from history.
    // CancellationToken.None: the author aborting their request must not cancel delivery to others.
    private async Task PushAfterCommitAsync(
        int groupId,
        int courseId,
        int messageId,
        Func<IReadOnlyCollection<int>, CancellationToken, Task> push)
    {
        try
        {
            var recipients = await GetChatParticipantIdsAsync(groupId, courseId, CancellationToken.None);
            await push(recipients, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GroupChatService::PushAfterCommitAsync: real-time push failed for group {GroupId} message {MessageId}",
                groupId, messageId);
        }
    }

    // Everyone who may read the group right now; mirrors HasAccessAsync minus the admin bypass, which
    // the notifier applies per connection.
    private async Task<IReadOnlyCollection<int>> GetChatParticipantIdsAsync(int groupId, int courseId, CancellationToken cancellationToken)
    {
        var creatorId = await _dbContext.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => c.CreatedByUserId)
            .FirstOrDefaultAsync(cancellationToken);

        var instructorIds = await _dbContext.CourseInstructors
            .AsNoTracking()
            .Where(i => i.CourseId == courseId)
            .Select(i => i.UserId)
            .ToListAsync(cancellationToken);

        var memberIds = await _dbContext.StudentGroups
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .SelectMany(g => g.Members)
            .Select(m => m.UserId)
            .Where(userId => _dbContext.Enrollments.Any(e =>
                e.CourseId == courseId && e.UserId == userId && e.Status == EnrollmentStatus.Active))
            .ToListAsync(cancellationToken);

        return [.. instructorIds.Concat(memberIds).Append(creatorId).ToHashSet()];
    }

    private static GroupChatMessageDto CopyWithIsMine(GroupChatMessageDto dto, bool isMine) => new()
    {
        Id = dto.Id,
        GroupId = dto.GroupId,
        AuthorUserId = dto.AuthorUserId,
        AuthorName = dto.AuthorName,
        AuthorIsStaff = dto.AuthorIsStaff,
        Body = dto.Body,
        SentAt = dto.SentAt,
        IsDeleted = dto.IsDeleted,
        IsMine = isMine,
    };

    private async Task<int?> GetGroupCourseIdAsync(int groupId, CancellationToken cancellationToken)
    {
        var courseId = await _dbContext.StudentGroups
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .Select(g => g.CourseId)
            .FirstOrDefaultAsync(cancellationToken);

        return courseId == 0 ? null : courseId;
    }

    private async Task<bool> HasAccessAsync(int groupId, int courseId, int actingUserId, bool isAdmin, CancellationToken cancellationToken) =>
        isAdmin
        || await _dbContext.IsTeachingStaffAsync(courseId, actingUserId, cancellationToken)
        || await _dbContext.MemberGroupIds(actingUserId).AnyAsync(id => id == groupId, cancellationToken);

    private async Task EnsureAccessAsync(int groupId, int courseId, int actingUserId, bool isAdmin, CancellationToken cancellationToken)
    {
        if (!await HasAccessAsync(groupId, courseId, actingUserId, isAdmin, cancellationToken))
            throw new GroupAuthorizationException("You do not have access to this group's chat.");
    }
}
