using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Qna;
using LF.AppDomain.Entities.User;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomain.Models.Qna.Enums;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Qna;
using LF.Application.Services.Qna;
using LF.ApplicationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

// Sibling test namespaces (LF.ApplicationTests.Services.Course/.Enrollment) shadow the entity names
// from inside this namespace, so the domain types are aliased — the same trick EnrollmentService uses.
using DomainCourse = LF.AppDomain.Entities.Course.Course;
using DomainEnrollment = LF.AppDomain.Entities.Course.Enrollment;

namespace LF.ApplicationTests.Services.Qna;

public class LessonQuestionServiceTests
{
    // The service clock. Fixtures are built in the past so anything the service writes is
    // unambiguously the newest message in its thread — the realistic case, and it keeps the
    // "order by CreatedAt" projection deterministic without relying on DB-generated message ids.
    private static readonly DateTime Now = new(2026, 9, 18, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Asked = Now.AddHours(-3);

    // The cast of the access matrix. Ids are fixed so every test reads the same way.
    private const int CourseId = 7;
    private const int LessonId = 42;
    private const int OtherCourseId = 8;
    private const int OtherLessonId = 43;

    private const int StudentId = 100;
    private const int OtherStudentId = 101;
    private const int CreatorId = 200;
    private const int AssignedInstructorId = 201;
    private const int UnassignedInstructorId = 202;
    private const int AdminId = 300;

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class Harness
    {
        public required LessonQuestionService Service { get; init; }
        public required Mock<IAppDbContext> DbContext { get; init; }
        public required List<LessonQuestion> Questions { get; init; }
        public required List<LessonQuestionReadMarker> Markers { get; init; }
        public required Mock<DbSet<LessonQuestion>> QuestionsSet { get; init; }
    }

    private static Harness CreateHarness(
        IEnumerable<LessonQuestion>? questions = null,
        IEnumerable<DomainEnrollment>? enrollments = null,
        IEnumerable<CourseInstructor>? instructors = null,
        IEnumerable<LessonQuestionReadMarker>? markers = null,
        IEnumerable<DomainCourse>? courses = null)
    {
        var questionList = questions?.ToList() ?? [];
        var markerList = markers?.ToList() ?? [];

        var questionsSet = questionList.BuildMockDbSet();
        var markersSet = markerList.BuildMockDbSet();

        // The mocked DbSet does not write through to the backing list, so Add is wired up by hand;
        // otherwise AskAsync could never read back the thread it just created.
        questionsSet.Setup(s => s.Add(It.IsAny<LessonQuestion>()))
            .Callback<LessonQuestion>(q =>
            {
                EntityIdSetter.SetId(q, questionList.Count + 1);
                questionList.Add(q);
            });

        markersSet.Setup(s => s.Add(It.IsAny<LessonQuestionReadMarker>()))
            .Callback<LessonQuestionReadMarker>(markerList.Add);

        var dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.LessonQuestions).Returns(questionsSet.Object);
        dbContextMock.SetupGet(c => c.LessonQuestionReadMarkers).Returns(markersSet.Object);
        dbContextMock.SetupGet(c => c.Courses).Returns((courses?.ToList() ?? [DefaultCourse(), OtherCourse()]).BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.Enrollments).Returns((enrollments?.ToList() ?? []).BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.CourseInstructors).Returns((instructors?.ToList() ?? []).BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.Users).Returns(Users().BuildMockDbSet().Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new Harness
        {
            Service = new LessonQuestionService(NullLogger<LessonQuestionService>.Instance, dbContextMock.Object, new FixedTimeProvider(Now)),
            DbContext = dbContextMock,
            Questions = questionList,
            Markers = markerList,
            QuestionsSet = questionsSet,
        };
    }

    private static List<DbUser> Users() =>
    [
        new() { Id = StudentId, Email = "student@lf.test", FirstName = "Sasha", LastName = "Student", Role = UserRole.Student },
        new() { Id = OtherStudentId, Email = "other@lf.test", FirstName = "Olya", LastName = "Other", Role = UserRole.Student },
        new() { Id = CreatorId, Email = "creator@lf.test", FirstName = "Kim", LastName = "Creator", Role = UserRole.CourseCreator },
        new() { Id = AssignedInstructorId, Email = "assigned@lf.test", FirstName = "Ira", LastName = "Assigned", Role = UserRole.Instructor },
        new() { Id = UnassignedInstructorId, Email = "unassigned@lf.test", FirstName = "Ulya", LastName = "Unassigned", Role = UserRole.Instructor },
        new() { Id = AdminId, Email = "admin@lf.test", FirstName = "Ada", LastName = "Admin", Role = UserRole.Admin },
    ];

    private static DomainCourse BuildCourse(int courseId, int lessonId, int createdByUserId, string title)
    {
        var category = Category.Create("General", isDefault: true);
        EntityIdSetter.SetId(category, 1);

        var course = DomainCourse.Create(title, "Intro", "Description", category, createdByUserId, Now);
        EntityIdSetter.SetId(course, courseId);

        var chapter = course.AddChapter("Chapter 1");
        EntityIdSetter.SetId(chapter, courseId * 10);

        var lesson = chapter.AddLesson($"Lesson {lessonId}");
        EntityIdSetter.SetId(lesson, lessonId);

        return course;
    }

    private static DomainCourse DefaultCourse() => BuildCourse(CourseId, LessonId, CreatorId, "Kotlin Basics");

    private static DomainCourse OtherCourse() => BuildCourse(OtherCourseId, OtherLessonId, UnassignedInstructorId, "Rust Basics");

    private static DomainEnrollment ActiveEnrollment(int courseId = CourseId, int userId = StudentId)
    {
        var enrollment = DomainEnrollment.Create(courseId, userId, Now, EnrollmentStatus.Active);
        EntityIdSetter.SetId(enrollment, courseId * 100 + userId);
        return enrollment;
    }

    private static DomainEnrollment PendingEnrollment(int courseId = CourseId, int userId = StudentId)
    {
        var enrollment = DomainEnrollment.Create(courseId, userId, Now, EnrollmentStatus.PendingPayment);
        EntityIdSetter.SetId(enrollment, courseId * 100 + userId);
        return enrollment;
    }

    private static CourseInstructor Assignment(int courseId = CourseId, int userId = AssignedInstructorId)
    {
        var assignment = CourseInstructor.Create(courseId, userId, CreatorId, Now);
        EntityIdSetter.SetId(assignment, courseId * 100 + userId);
        return assignment;
    }

    private static LessonQuestion Question(
        int id = 1,
        int courseId = CourseId,
        int lessonId = LessonId,
        int studentUserId = StudentId,
        string title = "Why does this fail?")
    {
        var question = LessonQuestion.Ask(courseId, lessonId, studentUserId, title, "Body of the question.", Asked);
        EntityIdSetter.SetId(question, id);
        EntityIdSetter.SetId(question.Messages[0], id * 10);
        return question;
    }

    private static AskQuestionDto AskDto(int lessonId = LessonId) =>
        new() { LessonId = lessonId, Title = "Why does this fail?", Body = "Body of the question." };

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AskAsync_ActiveEnrollment_CreatesThread()
    {
        var harness = CreateHarness(enrollments: [ActiveEnrollment()]);

        var thread = await harness.Service.AskAsync(AskDto(), StudentId, Ct);

        Assert.NotNull(thread);
        Assert.Equal(CourseId, thread.CourseId);
        Assert.Equal(LessonId, thread.LessonId);
        Assert.Equal(StudentId, thread.StudentUserId);
        Assert.Equal("Kotlin Basics", thread.CourseTitle);
        Assert.Equal($"Lesson {LessonId}", thread.LessonTitle);
        Assert.Equal(nameof(LessonQuestionStatus.Open), thread.Status.ToString());
        Assert.Equal("Sasha Student", thread.StudentName);

        var message = Assert.Single(thread.Messages);
        Assert.Equal("Body of the question.", message.Body);
        Assert.Equal(QuestionAuthorRole.Student, message.AuthorRole);
    }

    [Fact]
    public async Task AskAsync_NotEnrolled_Throws()
    {
        var harness = CreateHarness();

        await Assert.ThrowsAsync<QuestionAuthorizationException>(() => harness.Service.AskAsync(AskDto(), StudentId, Ct));
        Assert.Empty(harness.Questions);
    }

    [Fact]
    public async Task AskAsync_PendingPaymentEnrollment_Throws()
    {
        var harness = CreateHarness(enrollments: [PendingEnrollment()]);

        await Assert.ThrowsAsync<QuestionAuthorizationException>(() => harness.Service.AskAsync(AskDto(), StudentId, Ct));
    }

    [Fact]
    public async Task AskAsync_EnrolledInADifferentCourse_Throws()
    {
        var harness = CreateHarness(enrollments: [ActiveEnrollment(OtherCourseId, StudentId)]);

        await Assert.ThrowsAsync<QuestionAuthorizationException>(() => harness.Service.AskAsync(AskDto(), StudentId, Ct));
    }

    [Fact]
    public async Task AskAsync_UnknownLesson_ReturnsNull()
    {
        var harness = CreateHarness(enrollments: [ActiveEnrollment()]);

        Assert.Null(await harness.Service.AskAsync(AskDto(lessonId: 9999), StudentId, Ct));
    }

    [Fact]
    public async Task GetThreadAsync_Author_ReturnsThread()
    {
        var harness = CreateHarness(questions: [Question()]);

        var thread = await harness.Service.GetThreadAsync(1, StudentId, isAdmin: false, Ct);

        Assert.NotNull(thread);
        Assert.Equal(1, thread.Id);
    }

    // The private-thread guarantee: enrolling in the course is not enough to read someone else's thread.
    [Fact]
    public async Task GetThreadAsync_OtherStudentsThread_Throws()
    {
        var harness = CreateHarness(questions: [Question()], enrollments: [ActiveEnrollment(CourseId, OtherStudentId)]);

        await Assert.ThrowsAsync<QuestionAuthorizationException>(
            () => harness.Service.GetThreadAsync(1, OtherStudentId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task GetThreadAsync_CourseCreator_ReturnsThread()
    {
        var harness = CreateHarness(questions: [Question()]);

        Assert.NotNull(await harness.Service.GetThreadAsync(1, CreatorId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task GetThreadAsync_AssignedInstructor_ReturnsThread()
    {
        var harness = CreateHarness(questions: [Question()], instructors: [Assignment()]);

        Assert.NotNull(await harness.Service.GetThreadAsync(1, AssignedInstructorId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task GetThreadAsync_InstructorNotAssignedToCourse_Throws()
    {
        var harness = CreateHarness(questions: [Question()], instructors: [Assignment(OtherCourseId, UnassignedInstructorId)]);

        await Assert.ThrowsAsync<QuestionAuthorizationException>(
            () => harness.Service.GetThreadAsync(1, UnassignedInstructorId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task GetThreadAsync_Admin_ReturnsAnyThread()
    {
        var harness = CreateHarness(questions: [Question()]);

        Assert.NotNull(await harness.Service.GetThreadAsync(1, AdminId, isAdmin: true, Ct));
    }

    [Fact]
    public async Task GetThreadAsync_UnknownThread_ReturnsNull()
    {
        var harness = CreateHarness();

        Assert.Null(await harness.Service.GetThreadAsync(404, StudentId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task GetThreadAsync_MarksTheThreadSeenForTheCaller()
    {
        var question = Question();
        question.AddMessage(CreatorId, QuestionAuthorRole.Instructor, "Here is the answer.", Asked.AddMinutes(30));
        var harness = CreateHarness(questions: [question]);

        var thread = await harness.Service.GetThreadAsync(1, StudentId, isAdmin: false, Ct);

        Assert.False(thread!.HasUnread);
        var marker = Assert.Single(harness.Markers);
        Assert.Equal(StudentId, marker.UserId);
        Assert.Equal(1, marker.LessonQuestionId);
    }

    [Fact]
    public async Task ListAsync_AsStudent_ReturnsOnlyOwnThreads()
    {
        var harness = CreateHarness(questions:
        [
            Question(id: 1, studentUserId: StudentId),
            Question(id: 2, studentUserId: OtherStudentId),
        ]);

        var result = await harness.Service.ListAsync(StudentId, LessonQuestionScope.AsStudent, null, null, 1, 10, Ct);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal([1], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task ListAsync_AsStaff_ReturnsOnlyCoursesTheUserTeaches()
    {
        var harness = CreateHarness(
            questions:
            [
                Question(id: 1, courseId: CourseId, lessonId: LessonId),
                Question(id: 2, courseId: OtherCourseId, lessonId: OtherLessonId, studentUserId: OtherStudentId),
            ],
            instructors: [Assignment(CourseId, AssignedInstructorId)]);

        var result = await harness.Service.ListAsync(AssignedInstructorId, LessonQuestionScope.AsStaff, null, null, 1, 10, Ct);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal([1], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task ListAsync_AsStaff_IncludesCoursesTheUserCreated()
    {
        var harness = CreateHarness(questions: [Question(id: 1)]);

        var result = await harness.Service.ListAsync(CreatorId, LessonQuestionScope.AsStaff, null, null, 1, 10, Ct);

        Assert.Equal([1], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task ListAsync_AsStaff_ForSomeoneWhoTeachesNothing_IsEmpty()
    {
        var harness = CreateHarness(questions: [Question(id: 1)]);

        var result = await harness.Service.ListAsync(UnassignedInstructorId, LessonQuestionScope.AsStaff, null, null, 1, 10, Ct);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task ListAsync_NewestActivityFirst()
    {
        var older = Question(id: 1, title: "Older");
        var newer = Question(id: 2, title: "Newer");
        newer.AddMessage(StudentId, QuestionAuthorRole.Student, "Bumping this.", Asked.AddMinutes(30));

        var harness = CreateHarness(questions: [older, newer]);

        var result = await harness.Service.ListAsync(StudentId, LessonQuestionScope.AsStudent, null, null, 1, 10, Ct);

        Assert.Equal([2, 1], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task ListAsync_StatusFilter_Applies()
    {
        var answered = Question(id: 2);
        answered.AddMessage(CreatorId, QuestionAuthorRole.Instructor, "Answered.", Asked.AddMinutes(30));

        var harness = CreateHarness(questions: [Question(id: 1), answered]);

        var result = await harness.Service.ListAsync(
            StudentId, LessonQuestionScope.AsStudent, null, LessonQuestionStatus.Answered, 1, 10, Ct);

        Assert.Equal([2], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task ListForLessonAsync_Student_SeesOnlyTheirOwnThreadsOnThatLesson()
    {
        var harness = CreateHarness(questions:
        [
            Question(id: 1, studentUserId: StudentId),
            Question(id: 2, studentUserId: OtherStudentId),
            Question(id: 3, courseId: OtherCourseId, lessonId: OtherLessonId, studentUserId: StudentId),
        ]);

        var result = await harness.Service.ListForLessonAsync(LessonId, StudentId, isAdmin: false, 1, 10, Ct);

        Assert.Equal([1], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task ListForLessonAsync_Staff_SeesEveryThreadOnThatLesson()
    {
        var harness = CreateHarness(questions:
        [
            Question(id: 1, studentUserId: StudentId),
            Question(id: 2, studentUserId: OtherStudentId),
        ]);

        var result = await harness.Service.ListForLessonAsync(LessonId, CreatorId, isAdmin: false, 1, 10, Ct);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task PostMessageAsync_AssignedInstructor_AnswersThread()
    {
        var harness = CreateHarness(questions: [Question()], instructors: [Assignment()]);

        var thread = await harness.Service.PostMessageAsync(
            1, new PostQuestionMessageDto { Body = "Try it this way." }, AssignedInstructorId, isAdmin: false, Ct);

        Assert.NotNull(thread);
        Assert.Equal(LessonQuestionStatus.Answered, thread.Status);
        Assert.Equal(2, thread.Messages.Count);
        Assert.Equal(QuestionAuthorRole.Instructor, thread.Messages[1].AuthorRole);
        Assert.Equal("Ira Assigned", thread.Messages[1].AuthorName);
    }

    [Fact]
    public async Task PostMessageAsync_StudentReply_ReopensAnsweredThread()
    {
        var question = Question();
        question.AddMessage(CreatorId, QuestionAuthorRole.Instructor, "Answered.", Asked.AddMinutes(30));
        var harness = CreateHarness(questions: [question]);

        var thread = await harness.Service.PostMessageAsync(
            1, new PostQuestionMessageDto { Body = "Still stuck." }, StudentId, isAdmin: false, Ct);

        Assert.Equal(LessonQuestionStatus.Open, thread!.Status);
    }

    // An admin who happens to be the author writes as a student, not as staff.
    [Fact]
    public async Task PostMessageAsync_AuthorWhoIsAlsoAdmin_WritesAsStudent()
    {
        var harness = CreateHarness(questions: [Question(studentUserId: AdminId)]);

        var thread = await harness.Service.PostMessageAsync(
            1, new PostQuestionMessageDto { Body = "Following up." }, AdminId, isAdmin: true, Ct);

        Assert.Equal(QuestionAuthorRole.Student, thread!.Messages[1].AuthorRole);
        Assert.Equal(LessonQuestionStatus.Open, thread.Status);
    }

    [Fact]
    public async Task PostMessageAsync_InstructorNotAssignedToCourse_Throws()
    {
        var harness = CreateHarness(questions: [Question()]);

        await Assert.ThrowsAsync<QuestionAuthorizationException>(() => harness.Service.PostMessageAsync(
            1, new PostQuestionMessageDto { Body = "Butting in." }, UnassignedInstructorId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task PostMessageAsync_ClosedThread_Throws()
    {
        var question = Question();
        question.Close();
        var harness = CreateHarness(questions: [question]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Service.PostMessageAsync(
            1, new PostQuestionMessageDto { Body = "Too late." }, StudentId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task PostMessageAsync_UnknownThread_ReturnsNull()
    {
        var harness = CreateHarness();

        Assert.Null(await harness.Service.PostMessageAsync(
            404, new PostQuestionMessageDto { Body = "Hello?" }, StudentId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task SetStatusAsync_AuthorCloses_ThenReopens()
    {
        var harness = CreateHarness(questions: [Question()]);

        var closed = await harness.Service.SetStatusAsync(1, close: true, StudentId, isAdmin: false, Ct);
        Assert.Equal(LessonQuestionStatus.Closed, closed!.Status);

        var reopened = await harness.Service.SetStatusAsync(1, close: false, StudentId, isAdmin: false, Ct);
        Assert.Equal(LessonQuestionStatus.Open, reopened!.Status);
    }

    [Fact]
    public async Task SetStatusAsync_WithoutAccess_Throws()
    {
        var harness = CreateHarness(questions: [Question()]);

        await Assert.ThrowsAsync<QuestionAuthorizationException>(
            () => harness.Service.SetStatusAsync(1, close: true, OtherStudentId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task SetStatusAsync_AlreadyClosed_Throws()
    {
        var question = Question();
        question.Close();
        var harness = CreateHarness(questions: [question]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Service.SetStatusAsync(1, close: true, StudentId, isAdmin: false, Ct));
    }

    [Fact]
    public async Task GetUnreadCountAsync_CountsStaffRepliesTheStudentHasNotRead()
    {
        var question = Question();
        question.AddMessage(CreatorId, QuestionAuthorRole.Instructor, "Answered.", Asked.AddMinutes(30));
        var harness = CreateHarness(questions: [question]);

        Assert.Equal(1, await harness.Service.GetUnreadCountAsync(StudentId, Ct));
    }

    // Writing a message must not light up your own badge.
    [Fact]
    public async Task GetUnreadCountAsync_ExcludesThreadsWhoseLastMessageTheViewerWrote()
    {
        var harness = CreateHarness(questions: [Question()]);

        Assert.Equal(0, await harness.Service.GetUnreadCountAsync(StudentId, Ct));
    }

    [Fact]
    public async Task GetUnreadCountAsync_ExcludesThreadsSeenSinceTheLastMessage()
    {
        var question = Question();
        question.AddMessage(CreatorId, QuestionAuthorRole.Instructor, "Answered.", Asked.AddMinutes(30));

        var harness = CreateHarness(
            questions: [question],
            markers: [LessonQuestionReadMarker.Create(1, StudentId, Asked.AddMinutes(45))]);

        Assert.Equal(0, await harness.Service.GetUnreadCountAsync(StudentId, Ct));
    }

    [Fact]
    public async Task GetUnreadCountAsync_CountsStaffInboxToo()
    {
        var harness = CreateHarness(questions: [Question()], instructors: [Assignment()]);

        Assert.Equal(1, await harness.Service.GetUnreadCountAsync(AssignedInstructorId, Ct));
    }

    [Fact]
    public async Task GetUnreadCountAsync_IgnoresCoursesTheUserDoesNotTeach()
    {
        var harness = CreateHarness(questions: [Question()]);

        Assert.Equal(0, await harness.Service.GetUnreadCountAsync(UnassignedInstructorId, Ct));
    }
}
