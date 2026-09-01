using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LF.AppDomain.Entities.Payment;
using LF.Application.ModelDto.Payment;
using LF.Infrastructure.Services.Payment;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LF.PaymentServiceTests;

public class RobokassaPaymentGatewayTests
{
    private static RobokassaPaymentGateway CreateGateway(string algo = "SHA256") =>
        new(NullLogger<RobokassaPaymentGateway>.Instance, Options.Create(new RobokassaOptions
        {
            MerchantLogin = "demo",
            Password1 = "pass1",
            Password2 = "pass2",
            HashAlgorithm = algo,
            IsTest = true,
            PaymentPageUrl = "https://auth.robokassa.ru/Merchant/Index.aspx",
            SuccessUrl = "https://x/success",
            FailUrl = "https://x/fail",
        }));

    private static PaymentOrder CreateOrder(int id, decimal amount)
    {
        var order = PaymentOrder.Create(1, 7, amount, "Test course", DateTime.UtcNow);
        typeof(PaymentOrder).GetProperty(nameof(PaymentOrder.Id))!.SetValue(order, id);
        return order;
    }

    private static string Hash(string algo, string raw)
    {
        var bytes = Encoding.UTF8.GetBytes(raw);
        byte[] hash = algo switch
        {
            "MD5" => MD5.HashData(bytes),
            "SHA256" => SHA256.HashData(bytes),
            "SHA512" => SHA512.HashData(bytes),
            _ => throw new ArgumentOutOfRangeException(nameof(algo)),
        };
        return Convert.ToHexString(hash);
    }

    [Fact]
    public void BuildRedirectUrl_signs_login_outsum_invid_password1()
    {
        // Arrange
        var gateway = CreateGateway();
        var order = CreateOrder(42, 100m);
        var expected = Hash("SHA256", "demo:100.00:42:pass1");

        // Act
        var url = gateway.BuildRedirectUrl(order);

        // Assert
        Assert.Contains($"SignatureValue={expected}", url);
        Assert.Contains("OutSum=100.00", url);
        Assert.Contains("InvId=42", url);
        Assert.Contains("IsTest=1", url);
    }

    [Fact]
    public void VerifyResultSignature_accepts_a_correct_password2_signature()
    {
        // Arrange
        var gateway = CreateGateway();
        var callback = new PaymentCallbackDto
        {
            OutSum = "100.00",
            InvId = 42,
            SignatureValue = Hash("SHA256", "100.00:42:pass2").ToLowerInvariant(),
        };

        // Act / Assert
        Assert.True(gateway.VerifyResultSignature(callback));
    }

    [Fact]
    public void VerifyResultSignature_rejects_a_tampered_signature()
    {
        var gateway = CreateGateway();
        var callback = new PaymentCallbackDto { OutSum = "100.00", InvId = 42, SignatureValue = "deadbeef" };

        Assert.False(gateway.VerifyResultSignature(callback));
    }

    [Fact]
    public void VerifyResultSignature_rejects_when_amount_differs()
    {
        var gateway = CreateGateway();
        var callback = new PaymentCallbackDto
        {
            OutSum = "999.00",
            InvId = 42,
            SignatureValue = Hash("SHA256", "100.00:42:pass2"),
        };

        Assert.False(gateway.VerifyResultSignature(callback));
    }

    [Fact]
    public void VerifySuccessSignature_uses_password1()
    {
        var gateway = CreateGateway();
        var callback = new PaymentCallbackDto
        {
            OutSum = "100.00",
            InvId = 42,
            SignatureValue = Hash("SHA256", "100.00:42:pass1"),
        };

        Assert.True(gateway.VerifySuccessSignature(callback));
        Assert.False(gateway.VerifyResultSignature(callback));
    }

    [Fact]
    public void Md5_algorithm_is_supported()
    {
        var gateway = CreateGateway("MD5");
        var order = CreateOrder(7, 50m);
        var expected = Hash("MD5", "demo:50.00:7:pass1");

        Assert.Contains($"SignatureValue={expected}", gateway.BuildRedirectUrl(order));
    }
}
