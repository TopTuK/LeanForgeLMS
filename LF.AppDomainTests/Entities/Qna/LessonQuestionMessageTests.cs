using LF.AppDomain.Entities.Qna;
using LF.AppDomain.Models.Qna.Enums;

namespace LF.AppDomainTests.Entities.Qna;

public class LessonQuestionMessageTests
{
    private static readonly DateTime Now = new(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);

    // Messages are only reachable through the aggregate, so they are exercised through it.
    private static LessonQuestionMessage Message(string body = "Body text") =>
        LessonQuestion.Ask(1, 2, 3, "Title", body, Now).Messages[0];

    [Fact]
    public void Create_TrimsBody() =>
        Assert.Equal("Body text", Message(body: "  Body text  ").Body);

    [Fact]
    public void Create_StartsUndeleted()
    {
        var message = Message();

        Assert.False(message.IsDeleted);
        Assert.Null(message.DeletedAt);
        Assert.Null(message.DeletedByUserId);
    }

    [Fact]
    public void Create_UnknownAuthorRole_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            LessonQuestion.Ask(1, 2, 3, "Title", "Body", Now)
                .AddMessage(4, (QuestionAuthorRole)99, "Body", Now));

    [Fact]
    public void Create_NonPositiveAuthor_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LessonQuestion.Ask(1, 2, 3, "Title", "Body", Now)
                .AddMessage(0, QuestionAuthorRole.Instructor, "Body", Now));

    [Fact]
    public void SoftDelete_MarksAndAttributes()
    {
        var message = Message();
        var deletedAt = Now.AddHours(2);

        message.SoftDelete(deletedByUserId: 11, deletedAt);

        Assert.True(message.IsDeleted);
        Assert.Equal(deletedAt, message.DeletedAt);
        Assert.Equal(11, message.DeletedByUserId);

        // The row and its body survive; only the projection withholds it.
        Assert.Equal("Body text", message.Body);
    }

    [Fact]
    public void SoftDelete_Twice_KeepsFirstAttribution()
    {
        var message = Message();
        message.SoftDelete(11, Now.AddHours(2));

        message.SoftDelete(12, Now.AddHours(3));

        Assert.Equal(11, message.DeletedByUserId);
        Assert.Equal(Now.AddHours(2), message.DeletedAt);
    }

    [Fact]
    public void SoftDelete_NonPositiveUser_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Message().SoftDelete(0, Now));
}
