using LF.AppDomain.Entities.Email;
using LF.AppDomain.Entities.User;
using LF.Application.Common.Email;
using LF.Application.Common.Interfaces;
using LF.Application.Common.Options;
using LF.Application.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace LF.ApplicationTests.Services.Notifications;

public class EnrollmentNotifierTests
{
    private static EnrollmentNotifier CreateNotifier(
        IReadOnlyCollection<DbUser> users,
        out Mock<IAppDbContext> dbContextMock,
        out Mock<DbSet<EmailMessage>> emailsMock)
    {
        var usersMock = users.ToList().BuildMockDbSet();
        emailsMock = new List<EmailMessage>().BuildMockDbSet();

        dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.Users).Returns(usersMock.Object);
        dbContextMock.SetupGet(c => c.EmailMessages).Returns(emailsMock.Object);

        var options = Microsoft.Extensions.Options.Options.Create(new AppUrlOptions { PublicBaseUrl = "https://lms.example.com/" });

        return new EnrollmentNotifier(
            NullLogger<EnrollmentNotifier>.Instance,
            dbContextMock.Object,
            new EmailTemplateRenderer(),
            options,
            TimeProvider.System);
    }

    private static DbUser User(string email, string firstName, string? language)
    {
        var user = new DbUser { Id = 7, Email = email, FirstName = firstName };
        if (language is not null)
            user.SetPreferredLanguage(language);
        return user;
    }

    [Fact]
    public async Task Stage_EnglishUser_AddsEnglishEmailWithoutSaving()
    {
        var notifier = CreateNotifier([User("ivan@example.com", "Ivan", "en")], out var dbContextMock, out var emailsMock);

        await notifier.StageEnrollmentConfirmationAsync(7, "Lean Basics");

        emailsMock.Verify(s => s.Add(It.Is<EmailMessage>(m =>
            m.ToAddress == "ivan@example.com"
            && m.ToName == "Ivan"
            && m.Subject == "You're enrolled in “Lean Basics”"
            && m.HtmlBody.Contains("https://lms.example.com/courses/active"))), Times.Once);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Stage_UserWithoutLanguage_GetsDefaultRussianEmail()
    {
        var notifier = CreateNotifier([User("ivan@example.com", "Иван", null)], out _, out var emailsMock);

        await notifier.StageEnrollmentConfirmationAsync(7, "Основы");

        emailsMock.Verify(s => s.Add(It.Is<EmailMessage>(m => m.Subject == "Вы записаны на курс «Основы»")), Times.Once);
    }

    [Fact]
    public async Task Stage_UserWithoutFirstName_GreetsByEmail()
    {
        var notifier = CreateNotifier([User("ivan@example.com", "", "en")], out _, out var emailsMock);

        await notifier.StageEnrollmentConfirmationAsync(7, "Lean Basics");

        emailsMock.Verify(s => s.Add(It.Is<EmailMessage>(m => m.HtmlBody.Contains("Hi ivan@example.com,"))), Times.Once);
    }

    [Fact]
    public async Task Stage_UnknownUser_SkipsSilently()
    {
        var notifier = CreateNotifier([], out _, out var emailsMock);

        await notifier.StageEnrollmentConfirmationAsync(7, "Lean Basics");

        emailsMock.Verify(s => s.Add(It.IsAny<EmailMessage>()), Times.Never);
    }

    [Fact]
    public async Task Stage_MalformedAddress_SkipsWithoutThrowing()
    {
        var notifier = CreateNotifier([User("not-an-email", "Ivan", "en")], out _, out var emailsMock);

        await notifier.StageEnrollmentConfirmationAsync(7, "Lean Basics");

        emailsMock.Verify(s => s.Add(It.IsAny<EmailMessage>()), Times.Never);
    }
}
