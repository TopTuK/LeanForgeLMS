using LF.AppDomain.Entities.Qna;
using LF.AppDomain.Models.Qna.Enums;

namespace LF.AppDomainTests.Entities.Qna;

public class LessonQuestionTests
{
    private static readonly DateTime Now = new(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);

    private const int CourseId = 7;
    private const int LessonId = 42;
    private const int StudentUserId = 3;

    private static LessonQuestion Ask(string title = "How does this work?", string body = "I am stuck on step two.") =>
        LessonQuestion.Ask(CourseId, LessonId, StudentUserId, title, body, Now);

    [Fact]
    public void Ask_SetsThreadAndSeedsTheFirstMessage()
    {
        var question = Ask();

        Assert.Equal(CourseId, question.CourseId);
        Assert.Equal(LessonId, question.LessonId);
        Assert.Equal(StudentUserId, question.StudentUserId);
        Assert.Equal("How does this work?", question.Title);
        Assert.Equal(LessonQuestionStatus.Open, question.Status);
        Assert.Equal(Now, question.CreatedAt);
        Assert.Equal(Now, question.LastMessageAt);
        Assert.Equal(StudentUserId, question.LastMessageAuthorUserId);

        var message = Assert.Single(question.Messages);
        Assert.Equal(StudentUserId, message.AuthorUserId);
        Assert.Equal(QuestionAuthorRole.Student, message.AuthorRole);
        Assert.Equal("I am stuck on step two.", message.Body);
    }

    [Fact]
    public void Ask_TrimsTitle()
    {
        var question = Ask(title: "  Padded  ");

        Assert.Equal("Padded", question.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ask_BlankTitle_Throws(string title) =>
        Assert.Throws<ArgumentException>(() => Ask(title: title));

    [Fact]
    public void Ask_TitleTooLong_Throws() =>
        Assert.Throws<ArgumentException>(() => Ask(title: new string('a', LessonQuestion.MaxTitleLength + 1)));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ask_BlankBody_Throws(string body) =>
        Assert.Throws<ArgumentException>(() => Ask(body: body));

    [Fact]
    public void Ask_BodyTooLong_Throws() =>
        Assert.Throws<ArgumentException>(() => Ask(body: new string('a', LessonQuestionMessage.MaxBodyLength + 1)));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Ask_NonPositiveIdentifiers_Throw(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LessonQuestion.Ask(value, LessonId, StudentUserId, "T", "B", Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => LessonQuestion.Ask(CourseId, value, StudentUserId, "T", "B", Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => LessonQuestion.Ask(CourseId, LessonId, value, "T", "B", Now));
    }

    [Fact]
    public void AddMessage_StaffReply_AnswersThreadAndStampsLastMessage()
    {
        var question = Ask();
        var repliedAt = Now.AddHours(1);

        question.AddMessage(authorUserId: 9, QuestionAuthorRole.Instructor, "Try restarting the exercise.", repliedAt);

        Assert.Equal(LessonQuestionStatus.Answered, question.Status);
        Assert.Equal(repliedAt, question.LastMessageAt);
        Assert.Equal(9, question.LastMessageAuthorUserId);
        Assert.Equal(2, question.Messages.Count);
    }

    [Fact]
    public void AddMessage_StudentFollowUp_ReopensAnsweredThread()
    {
        var question = Ask();
        question.AddMessage(9, QuestionAuthorRole.Instructor, "Try restarting.", Now.AddHours(1));

        question.AddMessage(StudentUserId, QuestionAuthorRole.Student, "That did not help.", Now.AddHours(2));

        Assert.Equal(LessonQuestionStatus.Open, question.Status);
        Assert.Equal(StudentUserId, question.LastMessageAuthorUserId);
    }

    [Fact]
    public void AddMessage_AdminReply_AnswersThread()
    {
        var question = Ask();

        question.AddMessage(1, QuestionAuthorRole.Admin, "Handled by support.", Now.AddHours(1));

        Assert.Equal(LessonQuestionStatus.Answered, question.Status);
    }

    [Fact]
    public void AddMessage_ClosedThread_Throws()
    {
        var question = Ask();
        question.Close();

        Assert.Throws<InvalidOperationException>(() =>
            question.AddMessage(9, QuestionAuthorRole.Instructor, "Too late.", Now.AddHours(1)));
    }

    [Fact]
    public void Close_AlreadyClosed_Throws()
    {
        var question = Ask();
        question.Close();

        Assert.Throws<InvalidOperationException>(question.Close);
    }

    [Fact]
    public void Reopen_NotClosed_Throws() =>
        Assert.Throws<InvalidOperationException>(Ask().Reopen);

    [Fact]
    public void Reopen_WithNoStaffReply_ReturnsToOpen()
    {
        var question = Ask();
        question.Close();

        question.Reopen();

        Assert.Equal(LessonQuestionStatus.Open, question.Status);
    }

    [Fact]
    public void Reopen_AfterStaffReplied_ReturnsToAnswered()
    {
        var question = Ask();
        question.AddMessage(9, QuestionAuthorRole.Instructor, "Answered.", Now.AddHours(1));
        question.Close();

        question.Reopen();

        Assert.Equal(LessonQuestionStatus.Answered, question.Status);
    }
}
