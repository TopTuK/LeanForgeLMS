using LF.AppDomain.Entities.Payment;
using LF.AppDomain.Models.Payment.Enums;

namespace LF.AppDomainTests.Entities.Payment;

public class PaymentOrderTests
{
    private static PaymentOrder CreateOrder(decimal amount = 1000m) =>
        PaymentOrder.Create(enrollmentId: 5, userId: 7, amount, "Test course", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Create_ValidArgs_SetsPendingOrder()
    {
        var order = CreateOrder(1799.5m);

        Assert.Equal(5, order.EnrollmentId);
        Assert.Equal(7, order.UserId);
        Assert.Equal(1799.5m, order.Amount);
        Assert.Equal("Test course", order.Description);
        Assert.Equal(PaymentOrder.RobokassaProvider, order.Provider);
        Assert.Equal(PaymentOrderStatus.Pending, order.Status);
        Assert.Null(order.PaidAt);
        Assert.True(order.IsOpen);
    }

    [Theory]
    [InlineData(0, 7, 100)]
    [InlineData(5, 0, 100)]
    [InlineData(5, 7, 0)]
    [InlineData(5, 7, -1)]
    public void Create_InvalidArgs_Throws(int enrollmentId, int userId, decimal amount)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            PaymentOrder.Create(enrollmentId, userId, amount, "x", DateTime.UtcNow));
    }

    [Fact]
    public void Create_BlankDescription_Throws()
    {
        Assert.Throws<ArgumentException>(() => PaymentOrder.Create(1, 1, 10m, "   ", DateTime.UtcNow));
    }

    [Fact]
    public void Create_TruncatesOverlongDescription()
    {
        var order = PaymentOrder.Create(1, 1, 10m, new string('x', 500), DateTime.UtcNow);

        Assert.Equal(PaymentOrder.MaxDescriptionLength, order.Description.Length);
    }

    [Fact]
    public void MarkPaid_FromPending_SettlesAndReturnsTrue()
    {
        var order = CreateOrder(1000m);
        var now = new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc);

        var settled = order.MarkPaid(1000m, now, "op-123");

        Assert.True(settled);
        Assert.Equal(PaymentOrderStatus.Paid, order.Status);
        Assert.Equal(now, order.PaidAt);
        Assert.Equal("op-123", order.ProviderOperationId);
    }

    [Fact]
    public void MarkPaid_SecondCall_IsIdempotentAndReturnsFalse()
    {
        var order = CreateOrder(1000m);
        order.MarkPaid(1000m, DateTime.UtcNow);

        var settledAgain = order.MarkPaid(1000m, DateTime.UtcNow);

        Assert.False(settledAgain);
        Assert.Equal(PaymentOrderStatus.Paid, order.Status);
    }

    [Fact]
    public void MarkPaid_AmountMismatch_Throws()
    {
        var order = CreateOrder(1000m);

        Assert.Throws<InvalidOperationException>(() => order.MarkPaid(999m, DateTime.UtcNow));
        Assert.Equal(PaymentOrderStatus.Pending, order.Status);
    }

    [Fact]
    public void MarkPaid_AfterFailed_Throws()
    {
        var order = CreateOrder(1000m);
        order.MarkFailed();

        Assert.Throws<InvalidOperationException>(() => order.MarkPaid(1000m, DateTime.UtcNow));
    }

    [Fact]
    public void MarkFailed_And_MarkCancelled_OnlyFromPending()
    {
        var failed = CreateOrder();
        failed.MarkFailed();
        Assert.Equal(PaymentOrderStatus.Failed, failed.Status);
        Assert.Throws<InvalidOperationException>(() => failed.MarkCancelled());

        var cancelled = CreateOrder();
        cancelled.MarkCancelled();
        Assert.Equal(PaymentOrderStatus.Cancelled, cancelled.Status);
    }
}
