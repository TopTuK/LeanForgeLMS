using LF.AppDomain.Entities.Payment;
using LF.AppDomain.Models.Payment.Enums;
using LF.ApplicationTests.TestSupport;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Payment;
using LF.Application.Services.Payment;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace LF.ApplicationTests.Services.Payment;

public class PaymentOrderServiceTests
{
    private static PaymentOrderService CreateService(
        IReadOnlyCollection<PaymentOrder> orders,
        out Mock<IAppDbContext> dbContextMock,
        out Mock<IPaymentGateway> gatewayMock)
    {
        var ordersMock = orders.ToList().BuildMockDbSet();

        dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.PaymentOrders).Returns(ordersMock.Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        gatewayMock = new Mock<IPaymentGateway>();
        gatewayMock.Setup(g => g.BuildRedirectUrl(It.IsAny<PaymentOrder>())).Returns("https://pay/redirect");

        return new PaymentOrderService(NullLogger<PaymentOrderService>.Instance, dbContextMock.Object, gatewayMock.Object, TimeProvider.System);
    }

    private static PaymentOrder OrderWithId(int id, int enrollmentId, int userId, decimal amount, PaymentOrderStatus status = PaymentOrderStatus.Pending)
    {
        var order = PaymentOrder.Create(enrollmentId, userId, amount, "Course", DateTime.UtcNow);
        EntityIdSetter.SetId(order, id);
        if (status == PaymentOrderStatus.Paid)
            order.MarkPaid(amount, DateTime.UtcNow);
        return order;
    }

    private static PaymentCallbackDto Callback(int invId, string outSum, string signature = "sig") =>
        new() { InvId = invId, OutSum = outSum, SignatureValue = signature };

    [Fact]
    public async Task CreateOrderAsync_NoExistingOrder_PersistsAndReturnsRedirectUrl()
    {
        var service = CreateService([], out var dbContextMock, out var gatewayMock);

        var result = await service.CreateOrderAsync(new CreatePaymentOrderDto
        {
            EnrollmentId = 5,
            UserId = 7,
            Amount = 1000m,
            Description = "Course",
        });

        Assert.Equal("https://pay/redirect", result.PaymentUrl);
        Assert.Equal(PaymentOrderStatus.Pending, result.Status);
        gatewayMock.Verify(g => g.BuildRedirectUrl(It.IsAny<PaymentOrder>()), Times.Once);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_ExistingPendingOrder_ReusesWithoutPersisting()
    {
        var existing = OrderWithId(42, 5, 7, 1000m);
        var service = CreateService([existing], out var dbContextMock, out _);

        var result = await service.CreateOrderAsync(new CreatePaymentOrderDto
        {
            EnrollmentId = 5,
            UserId = 7,
            Amount = 1000m,
            Description = "Course",
        });

        Assert.Equal(42, result.Id);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrderAsync_WrongOwner_ReturnsNull()
    {
        var order = OrderWithId(42, 5, 7, 1000m);
        var service = CreateService([order], out _, out _);

        Assert.Null(await service.GetOrderAsync(42, actingUserId: 999));
        Assert.NotNull(await service.GetOrderAsync(42, actingUserId: 7));
    }

    [Fact]
    public async Task ConfirmAsync_ValidSignature_SettlesOrder()
    {
        var order = OrderWithId(42, 5, 7, 1000m);
        var service = CreateService([order], out var dbContextMock, out var gatewayMock);
        gatewayMock.Setup(g => g.VerifyResultSignature(It.IsAny<PaymentCallbackDto>())).Returns(true);

        var result = await service.ConfirmAsync(Callback(42, "1000.00"));

        Assert.True(result.WasNewlyPaid);
        Assert.Equal(5, result.EnrollmentId);
        Assert.Equal(1000m, result.AmountPaid);
        Assert.Equal(PaymentOrderStatus.Paid, order.Status);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmAsync_BadSignature_Throws()
    {
        var order = OrderWithId(42, 5, 7, 1000m);
        var service = CreateService([order], out _, out var gatewayMock);
        gatewayMock.Setup(g => g.VerifyResultSignature(It.IsAny<PaymentCallbackDto>())).Returns(false);

        await Assert.ThrowsAsync<PaymentSignatureException>(() => service.ConfirmAsync(Callback(42, "1000.00")));
    }

    [Fact]
    public async Task ConfirmAsync_UnknownOrder_Throws()
    {
        var service = CreateService([], out _, out var gatewayMock);
        gatewayMock.Setup(g => g.VerifyResultSignature(It.IsAny<PaymentCallbackDto>())).Returns(true);

        await Assert.ThrowsAsync<PaymentOrderNotFoundException>(() => service.ConfirmAsync(Callback(99, "1000.00")));
    }

    [Fact]
    public async Task ConfirmAsync_AmountMismatch_Throws()
    {
        var order = OrderWithId(42, 5, 7, 1000m);
        var service = CreateService([order], out _, out var gatewayMock);
        gatewayMock.Setup(g => g.VerifyResultSignature(It.IsAny<PaymentCallbackDto>())).Returns(true);

        await Assert.ThrowsAsync<PaymentAmountMismatchException>(() => service.ConfirmAsync(Callback(42, "500.00")));
    }

    [Fact]
    public async Task ConfirmAsync_AlreadyPaid_IsIdempotent()
    {
        var order = OrderWithId(42, 5, 7, 1000m, PaymentOrderStatus.Paid);
        var service = CreateService([order], out var dbContextMock, out var gatewayMock);
        gatewayMock.Setup(g => g.VerifyResultSignature(It.IsAny<PaymentCallbackDto>())).Returns(true);

        var result = await service.ConfirmAsync(Callback(42, "1000.00"));

        Assert.False(result.WasNewlyPaid);
        Assert.Equal(5, result.EnrollmentId);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
