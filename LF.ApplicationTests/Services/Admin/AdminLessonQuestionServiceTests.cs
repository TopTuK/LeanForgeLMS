using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Qna;
using LF.AppDomain.Entities.User;
using LF.AppDomain.Models.Qna.Enums;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Qna;
using LF.Application.Services.Admin;
using LF.ApplicationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

using DomainCourse = LF.AppDomain.Entities.Course.Course;
using DomainEnrollment = LF.AppDomain.Entities.Course.Enrollment;

namespace LF.ApplicationTests.Services.Admin;

public class AdminLessonQuestionServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Asked = Now.AddHours(-3);

    private const int CourseId = 7;
    private const int LessonId = 42;
    private const int OtherCourseId = 8;
    private const int OtherLessonId = 43;

    private const int StudentId = 100;
    private const int CreatorId = 200;
    private const int AdminId = 300;

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class Harness
    {
        public required AdminLessonQuestionService Service { get; init; }
        public required List<LessonQuestion> Questions { get; init; }
        public required Mock<DbSet<LessonQuestion>> QuestionsSet { get; init; }
    }

    private static Harness CreateHarness(params LessonQuestion[] questions)
    {
        var questionList = questions.ToList();
        var questionsSet = questionList.BuildMockDbSet();

        questionsSet.Setup(s => s.Remove(It.IsAny<LessonQuestion>()))
            .Callback<LessonQuestion>(q => questionList.Remove(q));

        var dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.LessonQuestions).Returns(questionsSet.Object);
        dbContextMock.SetupGet(c => c.LessonQuestionReadMarkers).Returns(new List<LessonQuestionReadMarker>().BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.Courses).Returns(Courses().BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.Users).Returns(Users().BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.Enrollments).Returns(new List<DomainEnrollment>().BuildMockDbSet().Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new Harness
        {
            Service = new AdminLessonQuestionService(
                NullLogger<AdminLessonQuestionService>.Instance, dbContextMock.Object, new FixedTimeProvider(Now)),
            Questions = questionList,
            QuestionsSet = questionsSet,
        };
    }

    private static List<DbUser> Users() =>
    [
        new() { Id = StudentId, Email = "student@lf.test", FirstName = "Sasha", LastName = "Student", Role = UserRole.Student },
        new() { Id = CreatorId, Email = "creator@lf.test", FirstName = "Kim", LastName = "Creator", Role = UserRole.CourseCreator },
        new() { Id = AdminId, Email = "admin@lf.test", FirstName = "Ada", LastName = "Admin", Role = UserRole.Admin },
    ];

    private static List<DomainCourse> Courses() =>
    [
        BuildCourse(CourseId, LessonId, "Kotlin Basics"),
        BuildCourse(OtherCourseId, OtherLessonId, "Rust Basics"),
    ];

    private static DomainCourse BuildCourse(int courseId, int lessonId, string title)
    {
        var category = Category.Create("General", isDefault: true);
        EntityIdSetter.SetId(category, 1);

        var course = DomainCourse.Create(title, "Intro", "Description", category, CreatorId, Asked);
        EntityIdSetter.SetId(course, courseId);

        var chapter = course.AddChapter("Chapter 1");
        EntityIdSetter.SetId(chapter, courseId * 10);

        var lesson = chapter.AddLesson($"Lesson {lessonId}");
        EntityIdSetter.SetId(lesson, lessonId);

        return course;
    }

    private static LessonQuestion Question(
        int id = 1,
        int courseId = CourseId,
        int lessonId = LessonId,
        string title = "Why does this fail?")
    {
        var question = LessonQuestion.Ask(courseId, lessonId, StudentId, title, "Body of the question.", Asked);
        EntityIdSetter.SetId(question, id);
        EntityIdSetter.SetId(question.Messages[0], id * 10);
        return question;
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ListAsync_ReturnsEveryThreadAcrossEveryCourse()
    {
        var harness = CreateHarness(Question(id: 1), Question(id: 2, courseId: OtherCourseId, lessonId: OtherLessonId));

        var result = await harness.Service.ListAsync(null, null, null, AdminId, 1, 10, Ct);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task ListAsync_CourseFilter_Applies()
    {
        var harness = CreateHarness(Question(id: 1), Question(id: 2, courseId: OtherCourseId, lessonId: OtherLessonId));

        var result = await harness.Service.ListAsync(OtherCourseId, null, null, AdminId, 1, 10, Ct);

        Assert.Equal([2], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task ListAsync_StatusFilter_Applies()
    {
        var answered = Question(id: 2);
        answered.AddMessage(CreatorId, QuestionAuthorRole.Instructor, "Answered.", Asked.AddMinutes(30));
        var harness = CreateHarness(Question(id: 1), answered);

        var result = await harness.Service.ListAsync(null, LessonQuestionStatus.Answered, null, AdminId, 1, 10, Ct);

        Assert.Equal([2], result.Items.Select(i => i.Id));
    }

    [Theory]
    [InlineData("deadlock")]
    [InlineData("DEADLOCK")]
    public async Task ListAsync_SearchMatchesTitleCaseInsensitively(string search)
    {
        var harness = CreateHarness(Question(id: 1, title: "A deadlock in chapter one"), Question(id: 2, title: "Unrelated"));

        var result = await harness.Service.ListAsync(null, null, search, AdminId, 1, 10, Ct);

        Assert.Equal([1], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task ListAsync_SearchMatchesCourseTitle()
    {
        var harness = CreateHarness(Question(id: 1), Question(id: 2, courseId: OtherCourseId, lessonId: OtherLessonId));

        var result = await harness.Service.ListAsync(null, null, "rust", AdminId, 1, 10, Ct);

        Assert.Equal([2], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task ListAsync_SearchMatchesStudentNameAndEmail()
    {
        var harness = CreateHarness(Question(id: 1));

        Assert.Equal([1], (await harness.Service.ListAsync(null, null, "sasha", AdminId, 1, 10, Ct)).Items.Select(i => i.Id));
        Assert.Equal([1], (await harness.Service.ListAsync(null, null, "student@lf.test", AdminId, 1, 10, Ct)).Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetThreadAsync_ReturnsAnyThread()
    {
        var harness = CreateHarness(Question());

        var thread = await harness.Service.GetThreadAsync(1, AdminId, Ct);

        Assert.NotNull(thread);
        Assert.Equal("Kotlin Basics", thread.CourseTitle);
        Assert.Equal("Sasha Student", thread.StudentName);
    }

    [Fact]
    public async Task GetThreadAsync_UnknownThread_ReturnsNull() =>
        Assert.Null(await CreateHarness().Service.GetThreadAsync(404, AdminId, Ct));

    [Fact]
    public async Task PostMessageAsync_WritesAsAdmin()
    {
        var harness = CreateHarness(Question());

        var thread = await harness.Service.PostMessageAsync(1, new PostQuestionMessageDto { Body = "Handled by support." }, AdminId, Ct);

        Assert.NotNull(thread);
        Assert.Equal(LessonQuestionStatus.Answered, thread.Status);
        Assert.Equal(QuestionAuthorRole.Admin, thread.Messages[1].AuthorRole);
        Assert.Equal("Ada Admin", thread.Messages[1].AuthorName);
    }

    [Fact]
    public async Task PostMessageAsync_ClosedThread_Throws()
    {
        var question = Question();
        question.Close();
        var harness = CreateHarness(question);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.PostMessageAsync(1, new PostQuestionMessageDto { Body = "Too late." }, AdminId, Ct));
    }

    [Fact]
    public async Task PostMessageAsync_UnknownThread_ReturnsNull() =>
        Assert.Null(await CreateHarness().Service.PostMessageAsync(404, new PostQuestionMessageDto { Body = "?" }, AdminId, Ct));

    // Deletion is soft: the thread keeps its shape, the body is withheld.
    [Fact]
    public async Task DeleteMessageAsync_SoftDeletesAndKeepsThreadIntact()
    {
        var question = Question();
        question.AddMessage(CreatorId, QuestionAuthorRole.Instructor, "Rude reply.", Asked.AddMinutes(30));
        EntityIdSetter.SetId(question.Messages[1], 11);
        var harness = CreateHarness(question);

        var thread = await harness.Service.DeleteMessageAsync(1, 11, AdminId, Ct);

        Assert.NotNull(thread);
        Assert.Equal(2, thread.Messages.Count);

        var deleted = thread.Messages.Single(m => m.Id == 11);
        Assert.True(deleted.IsDeleted);
        Assert.Null(deleted.Body);

        // The surviving message is untouched.
        Assert.Equal("Body of the question.", thread.Messages.Single(m => m.Id == 10).Body);
    }

    [Fact]
    public async Task DeleteMessageAsync_UnknownMessage_ReturnsNull()
    {
        var harness = CreateHarness(Question());

        Assert.Null(await harness.Service.DeleteMessageAsync(1, 999, AdminId, Ct));
    }

    [Fact]
    public async Task DeleteThreadAsync_RemovesTheThread()
    {
        var harness = CreateHarness(Question());

        Assert.True(await harness.Service.DeleteThreadAsync(1, Ct));
        Assert.Empty(harness.Questions);
        harness.QuestionsSet.Verify(s => s.Remove(It.Is<LessonQuestion>(q => q.Id == 1)), Times.Once);
    }

    [Fact]
    public async Task DeleteThreadAsync_UnknownThread_ReturnsFalse() =>
        Assert.False(await CreateHarness().Service.DeleteThreadAsync(404, Ct));
}
