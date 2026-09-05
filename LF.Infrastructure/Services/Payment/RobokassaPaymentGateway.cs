using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LF.AppDomain.Entities.Payment;
using LF.Application.ModelDto.Payment;
using LF.Application.Services.Payment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LF.Infrastructure.Services.Payment;

// Robokassa "classic" hosted checkout: we build a signed redirect URL (no outbound HTTP call), and
// the authoritative confirmation arrives on the ResultURL webhook, which LF.WebApi forwards here.
// Signature: HASH(MerchantLogin:OutSum:InvId[:Receipt]:Password1) on init;
//            HASH(OutSum:InvId:Password2) on ResultURL; HASH(OutSum:InvId:Password1) on SuccessURL.
internal sealed class RobokassaPaymentGateway(
    ILogger<RobokassaPaymentGateway> logger,
    IOptions<RobokassaOptions> options) : IPaymentGateway
{
    private readonly ILogger<RobokassaPaymentGateway> _logger = logger;
    private readonly RobokassaOptions _options = options.Value;

    public string BuildRedirectUrl(PaymentOrder order)
    {
        EnsureConfigured();

        var outSum = order.Amount.ToString("0.00", CultureInfo.InvariantCulture);
        var invId = order.Id.ToString(CultureInfo.InvariantCulture);

        string? receipt = _options.Fiscalization.Enabled ? BuildReceiptJson(order) : null;
        var encodedReceipt = receipt is null ? null : Uri.EscapeDataString(receipt);

        var signatureBase = encodedReceipt is null
            ? $"{_options.MerchantLogin}:{outSum}:{invId}:{_options.Password1}"
            : $"{_options.MerchantLogin}:{outSum}:{invId}:{encodedReceipt}:{_options.Password1}";

        var parameters = new List<KeyValuePair<string, string>>
        {
            new("MerchantLogin", _options.MerchantLogin),
            new("OutSum", outSum),
            new("InvId", invId),
            new("Description", order.Description),
            new("SignatureValue", ComputeHash(signatureBase)),
            new("Culture", _options.Culture),
            new("Encoding", "utf-8"),
        };

        if (receipt is not null)
            parameters.Add(new("Receipt", receipt));

        if (_options.IsTest)
            parameters.Add(new("IsTest", "1"));

        var query = string.Join('&', parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        return $"{_options.PaymentPageUrl}?{query}";
    }

    public bool VerifyResultSignature(PaymentCallbackDto callback)
    {
        EnsureConfigured();
        var raw = $"{callback.OutSum}:{callback.InvId}:{_options.Password2}{BuildShpSuffix(callback.ShpParams)}";
        return HashMatches(raw, callback.SignatureValue);
    }

    public bool VerifySuccessSignature(PaymentCallbackDto callback)
    {
        EnsureConfigured();
        var raw = $"{callback.OutSum}:{callback.InvId}:{_options.Password1}{BuildShpSuffix(callback.ShpParams)}";
        return HashMatches(raw, callback.SignatureValue);
    }

    private static string BuildShpSuffix(IReadOnlyDictionary<string, string>? shpParams)
    {
        if (shpParams is null || shpParams.Count == 0)
            return string.Empty;

        return string.Concat(shpParams
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $":{kv.Key}={kv.Value}"));
    }

    private bool HashMatches(string raw, string? provided) =>
        !string.IsNullOrWhiteSpace(provided)
        && string.Equals(ComputeHash(raw), provided.Trim(), StringComparison.OrdinalIgnoreCase);

    private string ComputeHash(string raw)
    {
        var bytes = Encoding.UTF8.GetBytes(raw);
        var hash = _options.HashAlgorithm.ToUpperInvariant() switch
        {
            "MD5" => MD5.HashData(bytes),
            "SHA1" => SHA1.HashData(bytes),
            "SHA256" => SHA256.HashData(bytes),
            "SHA512" => SHA512.HashData(bytes),
            _ => throw new InvalidOperationException($"Unsupported Robokassa hash algorithm '{_options.HashAlgorithm}'."),
        };

        return Convert.ToHexString(hash);
    }

    private string BuildReceiptJson(PaymentOrder order)
    {
        var receipt = new
        {
            items = new[]
            {
                new
                {
                    name = order.Description,
                    quantity = 1,
                    sum = order.Amount,
                    payment_method = _options.Fiscalization.PaymentMethod,
                    payment_object = _options.Fiscalization.PaymentObject,
                    tax = _options.Fiscalization.Tax,
                },
            },
        };

        return JsonSerializer.Serialize(receipt);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.MerchantLogin)
            || _options.MerchantLogin == "CHANGE_ME"
            || string.IsNullOrWhiteSpace(_options.Password1)
            || string.IsNullOrWhiteSpace(_options.Password2))
        {
            _logger.LogError("RobokassaPaymentGateway: Robokassa options are not configured (MerchantLogin/Password1/Password2).");
            throw new InvalidOperationException("Robokassa is not configured.");
        }
    }
}
