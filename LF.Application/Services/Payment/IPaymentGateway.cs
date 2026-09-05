using LF.AppDomain.Entities.Payment;
using LF.Application.ModelDto.Payment;

namespace LF.Application.Services.Payment;

// Provider abstraction (mirrors IFileStorageService). The Robokassa implementation lives in LF.Infrastructure.
public interface IPaymentGateway
{
    // The hosted-checkout URL the browser is redirected to for this order.
    string BuildRedirectUrl(PaymentOrder order);

    // Verifies the signature on the authoritative server-to-server ResultURL callback (uses password #2).
    bool VerifyResultSignature(PaymentCallbackDto callback);

    // Verifies the signature on the browser SuccessURL redirect (uses password #1).
    bool VerifySuccessSignature(PaymentCallbackDto callback);
}
