using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Groups;
using LF.AppDomain.Entities.User;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Interfaces;
using LF.ApplicationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;

using DomainCourse = LF.AppDomain.Entities.Course.Course;
using DomainEnrollment = LF.AppDomain.Entities.Course.Enrollment;

namespace LF.ApplicationTests.Services.Groups;

// A small in-memory course world shared by the group, lecture and chat service tests: one course with
// a creator, an assigned instructor, two enrolled students, one pending-payment student and an outsider.
internal sealed class GroupTestWorld
{
    public static readonly DateTime Now = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

    public const int CourseId = 7;
    public const int OtherCourseId = 8;
    public const int CreatorId = 200;
    public const int InstructorId = 201;
    public const int OtherInstructorId = 202;
    public const int StudentId = 100;
    public const int ClassmateId = 101;
    public const int PendingStudentId = 102;
    public const int OutsiderId = 103;
    public const int AdminId = 300;

    public List<DomainCourse> Courses { get; } = [];
    public List<CourseInstructor> CourseInstructors { get; } = [];
    public List<DbUser> Users { get; } = [];
    public List<DomainEnrollment> Enrollments { get; } = [];
    public List<StudentGroup> StudentGroups { get; } = [];
    public List<Lecture> Lectures { get; } = [];
    public List<GroupChatMessage> GroupChatMessages { get; } = [];
    public List<GroupChatReadMarker> GroupChatReadMarkers { get; } = [];

    public Mock<IAppDbContext> DbContext { get; } = new();

    public GroupTestWorld()
    {
        var category = Category.Create("General", isDefault: true);
        EntityIdSetter.SetId(category, 1);

        Courses.Add(Course(CourseId, "Kotlin Basics", category));
        Courses.Add(Course(OtherCourseId, "Rust Basics", category));

        var assignment = CourseInstructor.Create(CourseId, InstructorId, CreatorId, Now);
        EntityIdSetter.SetId(assignment, 1);
        CourseInstructors.Add(assignment);

        Users.AddRange(
        [
            new() { Id = StudentId, Email = "student@lf.test", FirstName = "Sasha", LastName = "Student", Role = UserRole.Student, CreatedAt = Now },
            new() { Id = ClassmateId, Email = "classmate@lf.test", FirstName = "Lena", LastName = "Classmate", Role = UserRole.Student, CreatedAt = Now },
            new() { Id = PendingStudentId, Email = "pending@lf.test", FirstName = "Petr", LastName = "Pending", Role = UserRole.Student, CreatedAt = Now },
            new() { Id = OutsiderId, Email = "outsider@lf.test", FirstName = "Oleg", LastName = "Outsider", Role = UserRole.Student, CreatedAt = Now },
            new() { Id = CreatorId, Email = "creator@lf.test", FirstName = "Kim", LastName = "Creator", Role = UserRole.CourseCreator, CreatedAt = Now },
            new() { Id = InstructorId, Email = "instructor@lf.test", FirstName = "Ira", LastName = "Assigned", Role = UserRole.Instructor, CreatedAt = Now },
            new() { Id = OtherInstructorId, Email = "other@lf.test", FirstName = "Ulya", LastName = "Other", Role = UserRole.Instructor, CreatedAt = Now },
            new() { Id = AdminId, Email = "admin@lf.test", FirstName = "Ada", LastName = "Admin", Role = UserRole.Admin, CreatedAt = Now },
        ]);

        Enroll(StudentId, CourseId);
        Enroll(ClassmateId, CourseId);
        Enroll(PendingStudentId, CourseId, EnrollmentStatus.PendingPayment);
        Enroll(OutsiderId, OtherCourseId);

        DbContext.SetupGet(c => c.Courses).Returns(() => Courses.BuildMockDbSet().Object);
        DbContext.SetupGet(c => c.CourseInstructors).Returns(() => CourseInstructors.BuildMockDbSet().Object);
        DbContext.SetupGet(c => c.Users).Returns(() => Users.BuildMockDbSet().Object);
        DbContext.SetupGet(c => c.Enrollments).Returns(() => Enrollments.BuildMockDbSet().Object);
        DbContext.SetupGet(c => c.StudentGroups).Returns(() => TrackedSet(StudentGroups).Object);
        DbContext.SetupGet(c => c.Lectures).Returns(() => TrackedSet(Lectures).Object);
        DbContext.SetupGet(c => c.GroupChatMessages).Returns(() => TrackedSet(GroupChatMessages).Object);
        DbContext.SetupGet(c => c.GroupChatReadMarkers).Returns(() => TrackedSet(GroupChatReadMarkers).Object);
        DbContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    public void Enroll(int userId, int courseId, EnrollmentStatus status = EnrollmentStatus.Active)
    {
        var enrollment = DomainEnrollment.Create(courseId, userId, Now, status);
        EntityIdSetter.SetId(enrollment, Enrollments.Count + 1);
        Enrollments.Add(enrollment);
    }

    public void Unenroll(int userId, int courseId) =>
        Enrollments.RemoveAll(e => e.UserId == userId && e.CourseId == courseId);

    public StudentGroup AddGroup(string name = "Stream A", int courseId = CourseId, params int[] memberIds)
    {
        var group = StudentGroup.Create(courseId, name, null, CreatorId, Now);
        EntityIdSetter.SetId(group, StudentGroups.Count + 1);
        foreach (var memberId in memberIds)
            group.AddMember(memberId, CreatorId, Now);

        StudentGroups.Add(group);
        return group;
    }

    public Lecture AddLecture(DateTime startsAt, int courseId = CourseId, params int[] groupIds)
    {
        var lecture = Lecture.Schedule(courseId, "Lecture", null, startsAt, 90, "https://meet.example/abc", groupIds, CreatorId, Now);
        EntityIdSetter.SetId(lecture, Lectures.Count + 1);
        Lectures.Add(lecture);
        return lecture;
    }

    public GroupChatMessage AddMessage(int groupId, int authorUserId, string body = "Hello")
    {
        var message = GroupChatMessage.Post(groupId, authorUserId, body, Now);
        EntityIdSetter.SetId(message, GroupChatMessages.Count + 1);
        GroupChatMessages.Add(message);
        return message;
    }

    // Add/Remove mutate the backing list so later queries in the same test see the change, and Add
    // assigns the id a real SaveChanges would.
    private static Mock<DbSet<T>> TrackedSet<T>(List<T> list) where T : class
    {
        var set = list.BuildMockDbSet();
        set.Setup(s => s.Add(It.IsAny<T>()))
            .Callback<T>(entity =>
            {
                EntityIdSetter.SetId(entity, list.Count + 1);
                list.Add(entity);
            });
        set.Setup(s => s.Remove(It.IsAny<T>()))
            .Callback<T>(entity => list.Remove(entity));
        return set;
    }

    private static DomainCourse Course(int id, string title, Category category)
    {
        var course = DomainCourse.Create(title, "Intro", "Description", category, CreatorId, Now);
        EntityIdSetter.SetId(course, id);
        return course;
    }
}
