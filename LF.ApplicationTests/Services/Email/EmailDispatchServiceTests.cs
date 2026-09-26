using LF.AppDomain.Entities.Email;
using LF.AppDomain.Models.Email.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Email;
using LF.Application.Services.Email;
using LF.ApplicationTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace LF.ApplicationTests.Services.Email;

public class EmailDispatchServiceTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private static EmailDispatchService CreateService(
        IReadOnlyCollection<EmailMessage> messages,
        out Mock<IAppDbContext> dbContextMock,
        out Mock<IEmailSender> senderMock)
    {
        var messagesMock = messages.ToList().BuildMockDbSet();

        dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.EmailMessages).Returns(messagesMock.Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        senderMock = new Mock<IEmailSender>();

        return new EmailDispatchService(
            NullLogger<EmailDispatchService>.Instance,
            dbContextMock.Object,
            senderMock.Object,
            new FixedTimeProvider(Now));
    }

    private static EmailMessage Message(int id, string to, DateTime? notBefore = null)
    {
        var message = EmailMessage.Create(to, null, "Subject", "<p>Body</p>", null, Now.AddMinutes(-10), notBefore);
        EntityIdSetter.SetId(message, id);
        return message;
    }

    [Fact]
    public async Task DispatchDueAsync_SendsOnlyDuePendingMessages()
    {
        var due = Message(1, "due@example.com");
        var future = Message(2, "future@example.com", Now.AddHours(1));
        var alreadySent = Message(3, "sent@example.com");
        alreadySent.MarkSent(Now.AddMinutes(-5));

        var service = CreateService([due, future, alreadySent], out _, out var senderMock);

        var result = await service.DispatchDueAsync(batchSize: 10, maxAttempts: 3);

        Assert.Equal(new EmailDispatchResult(1, 0, 0), result);
        senderMock.Verify(s => s.SendAsync(It.Is<OutgoingEmail>(e => e.ToAddress == "due@example.com"), It.IsAny<CancellationToken>()), Times.Once);
        senderMock.VerifyNoOtherCalls();
        Assert.Equal(EmailMessageStatus.Sent, due.Status);
        Assert.Equal(Now, due.SentAt);
        Assert.Equal(EmailMessageStatus.Pending, future.Status);
    }

    [Fact]
    public async Task DispatchDueAsync_RespectsBatchSize_OldestFirst()
    {
        var newer = Message(1, "newer@example.com", Now.AddMinutes(-1));
        var older = Message(2, "older@example.com");

        var service = CreateService([newer, older], out _, out var senderMock);

        var result = await service.DispatchDueAsync(batchSize: 1, maxAttempts: 3);

        Assert.Equal(1, result.Sent);
        Assert.Equal(EmailMessageStatus.Sent, older.Status);
        Assert.Equal(EmailMessageStatus.Pending, newer.Status);
    }

    [Fact]
    public async Task DispatchDueAsync_TransientFailure_SchedulesRetryAndContinuesBatch()
    {
        var flaky = Message(1, "flaky@example.com");
        var fine = Message(2, "fine@example.com");

        var service = CreateService([flaky, fine], out var dbContextMock, out var senderMock);
        senderMock
            .Setup(s => s.SendAsync(It.Is<OutgoingEmail>(e => e.ToAddress == "flaky@example.com"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailDeliveryException("SMTP connection failed", isTransient: true));

        var result = await service.DispatchDueAsync(batchSize: 10, maxAttempts: 3);

        Assert.Equal(new EmailDispatchResult(1, 1, 0), result);
        Assert.Equal(EmailMessageStatus.Pending, flaky.Status);
        Assert.Equal(1, flaky.Attempts);
        Assert.Equal("SMTP connection failed", flaky.LastError);
        Assert.True(flaky.NextAttemptAt > Now);
        Assert.Equal(EmailMessageStatus.Sent, fine.Status);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DispatchDueAsync_PermanentFailure_MarksFailed()
    {
        var rejected = Message(1, "nobody@example.com");

        var service = CreateService([rejected], out _, out var senderMock);
        senderMock
            .Setup(s => s.SendAsync(It.IsAny<OutgoingEmail>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailDeliveryException("SMTP 550 mailbox unavailable", isTransient: false));

        var result = await service.DispatchDueAsync(batchSize: 10, maxAttempts: 3);

        Assert.Equal(new EmailDispatchResult(0, 0, 1), result);
        Assert.Equal(EmailMessageStatus.Failed, rejected.Status);
    }

    [Fact]
    public async Task DispatchDueAsync_NothingDue_DoesNotSendOrSave()
    {
        var service = CreateService([Message(1, "later@example.com", Now.AddDays(1))], out var dbContextMock, out var senderMock);

        var result = await service.DispatchDueAsync(batchSize: 10, maxAttempts: 3);

        Assert.Equal(0, result.Total);
        senderMock.VerifyNoOtherCalls();
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(10, 0)]
    public async Task DispatchDueAsync_InvalidArgs_Throws(int batchSize, int maxAttempts)
    {
        var service = CreateService([], out _, out _);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.DispatchDueAsync(batchSize, maxAttempts));
    }
}
