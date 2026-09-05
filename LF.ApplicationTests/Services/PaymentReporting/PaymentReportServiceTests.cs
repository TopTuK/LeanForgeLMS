using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Payment;
using LF.AppDomain.Entities.User;
using LF.AppDomain.Models.Course.Enums;
using LF.AppDomain.Models.Payment.Enums;
using LF.ApplicationTests.TestSupport;
using LF.Application.Common.Interfaces;
using LF.Application.Services.PaymentReporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using DomainCourse = LF.AppDomain.Entities.Course.Course;
using DomainEnrollment = LF.AppDomain.Entities.Course.Enrollment;

namespace LF.ApplicationTests.Services.PaymentReporting;

public class PaymentReportServiceTests
{
    private static PaymentReportService CreateService(
        IReadOnlyCollection<PaymentOrder> orders,
        IReadOnlyCollection<DomainEnrollment> enrollments,
        IReadOnlyCollection<DomainCourse> courses,
        IReadOnlyCollection<DbUser> users,
        IReadOnlyCollection<CoursePayment> payments,
        out Mock<IAppDbContext> dbContextMock,
        out Mock<DbSet<CoursePayment>> paymentsSetMock,
        IReadOnlyCollection<PromoCode>? promoCodes = null)
    {
        paymentsSetMock = payments.ToList().BuildMockDbSet();

        dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.PaymentOrders).Returns(orders.ToList().BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.Enrollments).Returns(enrollments.ToList().BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.Courses).Returns(courses.ToList().BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.Users).Returns(users.ToList().BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.PromoCodes).Returns((promoCodes ?? []).ToList().BuildMockDbSet().Object);
        dbContextMock.SetupGet(c => c.CoursePayments).Returns(paymentsSetMock.Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new PaymentReportService(NullLogger<PaymentReportService>.Instance, dbContextMock.Object, TimeProvider.System);
    }

    private static PaymentOrder PaidOrder(int id, int enrollmentId, int userId, decimal amount)
    {
        var order = PaymentOrder.Create(enrollmentId, userId, amount, "Course", DateTime.UtcNow);
        EntityIdSetter.SetId(order, id);
        order.MarkPaid(amount, DateTime.UtcNow, "op-" + id);
        return order;
    }

    private static DomainEnrollment Enrollment(int id, int courseId, int userId, int? promoCodeId = null)
    {
        var enrollment = DomainEnrollment.Create(courseId, userId, DateTime.UtcNow, EnrollmentStatus.PendingPayment, 1000m, promoCodeId);
        EntityIdSetter.SetId(enrollment, id);
        return enrollment;
    }

    private static DomainCourse Course(int id, string title = "Clean Architecture")
    {
        var course = DomainCourse.Create(title, "Short", "Description", Category.Create("Backend"), createdByUserId: 1, DateTime.UtcNow);
        EntityIdSetter.SetId(course, id);
        return course;
    }

    private static DbUser User(int id, string email, string first, string last) =>
        new() { Id = id, Email = email, FirstName = first, LastName = last };

    [Fact]
    public async Task RecordCoursePaymentAsync_InsertsDenormalizedRow()
    {
        var service = CreateService(
            [PaidOrder(50, enrollmentId: 5, userId: 7, amount: 1990m)],
            [Enrollment(5, courseId: 3, userId: 7)],
            [Course(3, "Async in C#")],
            [User(7, "stud@pmi.moscow", "Ann", "Lee")],
            [],
            out _,
            out var paymentsSetMock);

        await service.RecordCoursePaymentAsync(50);

        paymentsSetMock.Verify(s => s.Add(It.Is<CoursePayment>(p =>
            p.PaymentOrderId == 50 &&
            p.CourseId == 3 &&
            p.UserId == 7 &&
            p.StudentEmail == "stud@pmi.moscow" &&
            p.StudentName == "Ann Lee" &&
            p.CourseTitle == "Async in C#" &&
            p.Amount == 1990m &&
            p.PromoCode == null &&
            p.ProviderOperationId == "op-50")), Times.Once);
    }

    [Fact]
    public async Task RecordCoursePaymentAsync_WithPromo_CapturesPromoCode()
    {
        var promo = PromoCode.Create("SAVE10", PromoCodeDiscountType.Percentage, 10m, 3, null, null, 1, DateTime.UtcNow);
        EntityIdSetter.SetId(promo, 11);
        var service = CreateService(
            [PaidOrder(50, 5, 7, 900m)],
            [Enrollment(5, courseId: 3, userId: 7, promoCodeId: 11)],
            [Course(3)],
            [User(7, "s@x.io", "A", "B")],
            [],
            out _,
            out var paymentsSetMock,
            promoCodes: [promo]);

        await service.RecordCoursePaymentAsync(50);

        paymentsSetMock.Verify(s => s.Add(It.Is<CoursePayment>(p => p.PromoCode == "SAVE10")), Times.Once);
    }

    [Fact]
    public async Task RecordCoursePaymentAsync_AlreadyRecorded_IsNoOp()
    {
        var existing = CoursePayment.Record(50, 5, 3, 7, "s@x.io", "A B", "Course", 1990m, null, "Robokassa", "op", DateTime.UtcNow, DateTime.UtcNow);
        var service = CreateService(
            [PaidOrder(50, 5, 7, 1990m)],
            [Enrollment(5, 3, 7)],
            [Course(3)],
            [User(7, "s@x.io", "A", "B")],
            [existing],
            out var dbContextMock,
            out var paymentsSetMock);

        await service.RecordCoursePaymentAsync(50);

        paymentsSetMock.Verify(s => s.Add(It.IsAny<CoursePayment>()), Times.Never);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReconcileAsync_InsertsOnlyMissingRows_AndReturnsCount()
    {
        var recorded = CoursePayment.Record(50, 5, 3, 7, "s@x.io", "A B", "Course", 1990m, null, "Robokassa", "op", DateTime.UtcNow, DateTime.UtcNow);
        var service = CreateService(
            [PaidOrder(50, 5, 7, 1990m), PaidOrder(51, 6, 8, 2990m)],
            [Enrollment(5, 3, 7), Enrollment(6, 4, 8)],
            [Course(3), Course(4)],
            [User(7, "a@x.io", "A", "A"), User(8, "b@x.io", "B", "B")],
            [recorded],
            out var dbContextMock,
            out var paymentsSetMock);

        var inserted = await service.ReconcileAsync();

        Assert.Equal(1, inserted);
        paymentsSetMock.Verify(s => s.Add(It.Is<CoursePayment>(p => p.PaymentOrderId == 51)), Times.Once);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
