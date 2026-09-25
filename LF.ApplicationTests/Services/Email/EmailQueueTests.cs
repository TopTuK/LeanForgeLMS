using LF.AppDomain.Entities.Email;
using LF.AppDomain.Models.Email.Enums;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Email;
using LF.Application.Services.Email;
using LF.ApplicationTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace LF.ApplicationTests.Services.Email;

public class EmailQueueTests
{
    private static EmailQueue CreateService(
        List<EmailMessage> messages,
        out Mock<IAppDbContext> dbContextMock,
        out Mock<Microsoft.EntityFrameworkCore.DbSet<EmailMessage>> messagesMock)
    {
        messagesMock = messages.BuildMockDbSet();

        dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.EmailMessages).Returns(messagesMock.Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new EmailQueue(NullLogger<EmailQueue>.Instance, dbContextMock.Object, TimeProvider.System);
    }

    [Fact]
    public async Task EnqueueAsync_AddsPendingMessageAndSaves()
    {
        var service = CreateService([], out var dbContextMock, out var messagesMock);

        var status = await service.EnqueueAsync(new EnqueueEmailDto
        {
            ToAddress = "student@example.com",
            Subject = "Welcome",
            HtmlBody = "<p>Hi</p>",
        });

        Assert.Equal("student@example.com", status.ToAddress);
        Assert.Equal(EmailMessageStatus.Pending, status.Status);
        messagesMock.Verify(s => s.Add(It.Is<EmailMessage>(m => m.Subject == "Welcome")), Times.Once);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnqueueAsync_InvalidAddress_ThrowsWithoutSaving()
    {
        var service = CreateService([], out var dbContextMock, out _);

        await Assert.ThrowsAsync<ArgumentException>(() => service.EnqueueAsync(new EnqueueEmailDto
        {
            ToAddress = "not-an-email",
            Subject = "Welcome",
            HtmlBody = "<p>Hi</p>",
        }));

        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsStatusOrNull()
    {
        var message = EmailMessage.Create("a@example.com", null, "S", "<p>b</p>", null, DateTime.UtcNow);
        EntityIdSetter.SetId(message, 7);
        var service = CreateService([message], out _, out _);

        var found = await service.GetStatusAsync(7);
        var missing = await service.GetStatusAsync(8);

        Assert.NotNull(found);
        Assert.Equal(7, found.Id);
        Assert.Null(missing);
    }
}
