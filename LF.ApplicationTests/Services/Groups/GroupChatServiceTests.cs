using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Groups;
using LF.Application.Services.Groups;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

using static LF.ApplicationTests.Services.Groups.GroupTestWorld;

namespace LF.ApplicationTests.Services.Groups;

public class GroupChatServiceTests
{
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private static GroupChatService CreateService(GroupTestWorld world, Mock<IGroupChatNotifier>? notifier = null) =>
        new(NullLogger<GroupChatService>.Instance, world.DbContext.Object, (notifier ?? new Mock<IGroupChatNotifier>()).Object, new FixedTimeProvider(Now));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PostAsync_Member_SavesAndBroadcastsViewerNeutralCopy()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId]);
        var notifier = new Mock<IGroupChatNotifier>();

        var message = await CreateService(world, notifier).PostAsync(group.Id, "  Hi all  ", StudentId, isAdmin: false, Ct);

        Assert.NotNull(message);
        Assert.Equal("Hi all", message.Body);
        Assert.True(message.IsMine);
        Assert.False(message.AuthorIsStaff);
        Assert.Equal("Sasha Student", message.AuthorName);
        Assert.Single(world.GroupChatMessages);
        notifier.Verify(n => n.MessagePostedAsync(
            It.Is<GroupChatMessageDto>(m => m.Id == message.Id && !m.IsMine),
            It.IsAny<IReadOnlyCollection<int>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Subscribers who lost access after joining the hub must be filtered out by the notifier, so the
    // recipient list has to reflect current access: staff plus members with an Active enrollment.
    [Fact]
    public async Task PostAsync_RecipientsAreStaffAndActivelyEnrolledMembersOnly()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId, ClassmateId]);
        world.Unenroll(ClassmateId, CourseId);
        var notifier = new Mock<IGroupChatNotifier>();
        IReadOnlyCollection<int>? recipients = null;
        notifier.Setup(n => n.MessagePostedAsync(It.IsAny<GroupChatMessageDto>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .Callback<GroupChatMessageDto, IReadOnlyCollection<int>, CancellationToken>((_, ids, _) => recipients = ids)
            .Returns(Task.CompletedTask);

        await CreateService(world, notifier).PostAsync(group.Id, "Hi", StudentId, isAdmin: false, Ct);

        Assert.NotNull(recipients);
        Assert.Equal([StudentId, CreatorId, InstructorId], recipients.Order());
    }

    // The message is already committed; a push failure must not turn the request into an error.
    [Fact]
    public async Task PostAsync_NotifierFailure_StillReturnsTheSavedMessage()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId]);
        var notifier = new Mock<IGroupChatNotifier>();
        notifier.Setup(n => n.MessagePostedAsync(It.IsAny<GroupChatMessageDto>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("hub down"));

        var message = await CreateService(world, notifier).PostAsync(group.Id, "Hi", StudentId, isAdmin: false, Ct);

        Assert.NotNull(message);
        Assert.Single(world.GroupChatMessages);
    }

    [Theory]
    [InlineData(CreatorId)]
    [InlineData(InstructorId)]
    public async Task PostAsync_TeachingStaff_IsFlaggedAsStaff(int staffId)
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId]);

        var message = await CreateService(world).PostAsync(group.Id, "Welcome", staffId, isAdmin: false, Ct);

        Assert.NotNull(message);
        Assert.True(message.AuthorIsStaff);
    }

    [Theory]
    [InlineData(ClassmateId)]
    [InlineData(OutsiderId)]
    [InlineData(OtherInstructorId)]
    public async Task PostAsync_NotMemberOrStaff_ThrowsAndDoesNotBroadcast(int userId)
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId]);
        var notifier = new Mock<IGroupChatNotifier>();

        await Assert.ThrowsAsync<GroupAuthorizationException>(() =>
            CreateService(world, notifier).PostAsync(group.Id, "Hi", userId, isAdmin: false, Ct));

        Assert.Empty(world.GroupChatMessages);
        notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetHistoryAsync_PagesBackwardsInReadingOrder()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId]);
        for (var i = 1; i <= 5; i++)
            world.AddMessage(group.Id, StudentId, $"m{i}");

        var service = CreateService(world);
        var newest = await service.GetHistoryAsync(group.Id, beforeMessageId: null, take: 2, StudentId, isAdmin: false, Ct);
        var older = await service.GetHistoryAsync(group.Id, beforeMessageId: 4, take: 2, StudentId, isAdmin: false, Ct);

        Assert.NotNull(newest);
        Assert.Equal(["m4", "m5"], newest.Items.Select(m => m.Body));
        Assert.True(newest.HasMore);
        Assert.NotNull(older);
        Assert.Equal(["m2", "m3"], older.Items.Select(m => m.Body));
    }

    [Fact]
    public async Task DeleteMessageAsync_StudentCannotDeleteSomeoneElsesMessage()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId, ClassmateId]);
        var message = world.AddMessage(group.Id, ClassmateId);

        await Assert.ThrowsAsync<GroupAuthorizationException>(() =>
            CreateService(world).DeleteMessageAsync(group.Id, message.Id, StudentId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task DeleteMessageAsync_Staff_ModeratesAndBroadcasts()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId]);
        var message = world.AddMessage(group.Id, StudentId);
        var notifier = new Mock<IGroupChatNotifier>();

        var deleted = await CreateService(world, notifier).DeleteMessageAsync(group.Id, message.Id, InstructorId, isAdmin: false, Ct);

        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
        Assert.Null(deleted.Body);
        notifier.Verify(n => n.MessageDeletedAsync(group.Id, message.Id, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteMessageAsync_NotifierFailure_StillReturnsTheDeletedMessage()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId]);
        var message = world.AddMessage(group.Id, StudentId);
        var notifier = new Mock<IGroupChatNotifier>();
        notifier.Setup(n => n.MessageDeletedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("hub down"));

        var deleted = await CreateService(world, notifier).DeleteMessageAsync(group.Id, message.Id, StudentId, isAdmin: false, Ct);

        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
    }

    [Fact]
    public async Task UnreadCounts_IgnoreOwnMessages_AndRespectTheReadMarker()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId, ClassmateId]);
        world.AddMessage(group.Id, ClassmateId);
        var second = world.AddMessage(group.Id, ClassmateId);
        world.AddMessage(group.Id, StudentId);
        world.AddMessage(group.Id, ClassmateId);
        var service = CreateService(world);

        Assert.Equal(3, Assert.Single(await service.GetUnreadCountsAsync(StudentId, Ct)).Count);

        Assert.True(await service.MarkReadAsync(group.Id, second.Id, StudentId, isAdmin: false, Ct));

        Assert.Equal(1, Assert.Single(await service.GetUnreadCountsAsync(StudentId, Ct)).Count);
    }

    [Fact]
    public async Task MarkReadAsync_ClampsToTheNewestMessage()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId]);
        var only = world.AddMessage(group.Id, CreatorId);

        await CreateService(world).MarkReadAsync(group.Id, 9_999, StudentId, isAdmin: false, Ct);

        Assert.Equal(only.Id, Assert.Single(world.GroupChatReadMarkers).LastSeenMessageId);
    }

    [Fact]
    public async Task CanAccessAsync_ReflectsMembershipStaffAndEnrollment()
    {
        var world = new GroupTestWorld();
        var group = world.AddGroup(memberIds: [StudentId]);
        var service = CreateService(world);

        Assert.True(await service.CanAccessAsync(group.Id, StudentId, isAdmin: false, Ct));
        Assert.True(await service.CanAccessAsync(group.Id, InstructorId, isAdmin: false, Ct));
        Assert.True(await service.CanAccessAsync(group.Id, OutsiderId, isAdmin: true, Ct));
        Assert.False(await service.CanAccessAsync(group.Id, ClassmateId, isAdmin: false, Ct));
        Assert.False(await service.CanAccessAsync(999, StudentId, isAdmin: false, Ct));

        world.Unenroll(StudentId, CourseId);
        Assert.False(await service.CanAccessAsync(group.Id, StudentId, isAdmin: false, Ct));
    }
}
