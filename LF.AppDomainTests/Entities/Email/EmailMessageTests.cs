using LF.AppDomain.Entities.Email;
using LF.AppDomain.Models.Email.Enums;

namespace LF.AppDomainTests.Entities.Email;

public class EmailMessageTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static EmailMessage CreateMessage(DateTime? notBefore = null) =>
        EmailMessage.Create("  student@example.com ", "  Ivan  ", " Welcome ", "<p>Hi</p>", "Hi", Now, notBefore);

    [Fact]
    public void Create_ValidArgs_SetsPendingMessageDueNow()
    {
        var message = CreateMessage();

        Assert.Equal("student@example.com", message.ToAddress);
        Assert.Equal("Ivan", message.ToName);
        Assert.Equal("Welcome", message.Subject);
        Assert.Equal(EmailMessageStatus.Pending, message.Status);
        Assert.Equal(0, message.Attempts);
        Assert.Equal(Now, message.NextAttemptAt);
        Assert.Equal(Now, message.CreatedAt);
        Assert.Null(message.SentAt);
        Assert.True(message.IsDue(Now));
    }

    [Fact]
    public void Create_WithFutureNotBefore_IsNotDueUntilThen()
    {
        var later = Now.AddHours(2);

        var message = CreateMessage(later);

        Assert.Equal(later, message.NextAttemptAt);
        Assert.False(message.IsDue(Now.AddHours(1)));
        Assert.True(message.IsDue(later));
    }

    [Fact]
    public void Create_WithPastNotBefore_IsDueImmediately()
    {
        var message = CreateMessage(Now.AddHours(-1));

        Assert.Equal(Now, message.NextAttemptAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("a@b@c")]
    [InlineData("us er@example.com")]
    public void Create_InvalidAddress_Throws(string address)
    {
        Assert.Throws<ArgumentException>(() => EmailMessage.Create(address, null, "Subject", "<p>x</p>", null, Now));
    }

    [Fact]
    public void Create_BlankSubject_Throws()
    {
        Assert.Throws<ArgumentException>(() => EmailMessage.Create("a@b.c", null, "  ", "<p>x</p>", null, Now));
    }

    [Fact]
    public void Create_OverlongSubject_Throws()
    {
        var subject = new string('s', EmailMessage.MaxSubjectLength + 1);

        Assert.Throws<ArgumentException>(() => EmailMessage.Create("a@b.c", null, subject, "<p>x</p>", null, Now));
    }

    [Fact]
    public void Create_BlankBody_Throws()
    {
        Assert.Throws<ArgumentException>(() => EmailMessage.Create("a@b.c", null, "Subject", " ", null, Now));
    }

    [Fact]
    public void MarkSent_FromPending_SetsSentAndCountsAttempt()
    {
        var message = CreateMessage();
        message.RecordFailure("temporary", Now, maxAttempts: 5, isTransient: true);

        message.MarkSent(Now.AddMinutes(2));

        Assert.Equal(EmailMessageStatus.Sent, message.Status);
        Assert.Equal(Now.AddMinutes(2), message.SentAt);
        Assert.Equal(2, message.Attempts);
        Assert.Null(message.LastError);
    }

    [Fact]
    public void MarkSent_WhenAlreadySent_Throws()
    {
        var message = CreateMessage();
        message.MarkSent(Now);

        Assert.Throws<InvalidOperationException>(() => message.MarkSent(Now));
    }

    [Fact]
    public void RecordFailure_Transient_SchedulesRetryWithGrowingBackoff()
    {
        var message = CreateMessage();

        message.RecordFailure("timeout", Now, maxAttempts: 10, isTransient: true);
        Assert.Equal(EmailMessageStatus.Pending, message.Status);
        Assert.Equal(Now.AddMinutes(1), message.NextAttemptAt);
        Assert.Equal("timeout", message.LastError);

        message.RecordFailure("timeout", Now, maxAttempts: 10, isTransient: true);
        Assert.Equal(Now.AddMinutes(5), message.NextAttemptAt);

        message.RecordFailure("timeout", Now, maxAttempts: 10, isTransient: true);
        Assert.Equal(Now.AddMinutes(15), message.NextAttemptAt);
        Assert.Equal(3, message.Attempts);
    }

    [Fact]
    public void RecordFailure_BeyondBackoffTable_ReusesLongestDelay()
    {
        var message = CreateMessage();

        for (var i = 0; i < 7; i++)
            message.RecordFailure("timeout", Now, maxAttempts: 10, isTransient: true);

        Assert.Equal(Now.AddHours(4), message.NextAttemptAt);
    }

    [Fact]
    public void RecordFailure_ReachingMaxAttempts_MarksFailed()
    {
        var message = CreateMessage();

        message.RecordFailure("timeout", Now, maxAttempts: 2, isTransient: true);
        message.RecordFailure("timeout", Now, maxAttempts: 2, isTransient: true);

        Assert.Equal(EmailMessageStatus.Failed, message.Status);
        Assert.Equal(2, message.Attempts);
        Assert.False(message.IsDue(Now.AddDays(1)));
    }

    [Fact]
    public void RecordFailure_Permanent_MarksFailedImmediately()
    {
        var message = CreateMessage();

        message.RecordFailure("550 mailbox unavailable", Now, maxAttempts: 5, isTransient: false);

        Assert.Equal(EmailMessageStatus.Failed, message.Status);
        Assert.Equal(1, message.Attempts);
    }

    [Fact]
    public void RecordFailure_TruncatesLongError()
    {
        var message = CreateMessage();

        message.RecordFailure(new string('e', EmailMessage.MaxErrorLength + 100), Now, maxAttempts: 5, isTransient: true);

        Assert.Equal(EmailMessage.MaxErrorLength, message.LastError!.Length);
    }

    [Fact]
    public void RecordFailure_WhenFailed_Throws()
    {
        var message = CreateMessage();
        message.RecordFailure("x", Now, maxAttempts: 1, isTransient: true);

        Assert.Throws<InvalidOperationException>(() => message.RecordFailure("x", Now, maxAttempts: 1, isTransient: true));
    }
}
