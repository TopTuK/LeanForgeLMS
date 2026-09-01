namespace LF.Application.ModelDto.Payment;

// Raw values as received from the provider on a ResultURL / SuccessURL callback, before verification.
public sealed class PaymentCallbackDto
{
    public string OutSum { get; init; } = null!;
    public int InvId { get; init; }
    public string SignatureValue { get; init; } = null!;

    // Provider "Shp_"/"shp_" custom params, echoed back on every callback and folded into the signature.
    public IReadOnlyDictionary<string, string> ShpParams { get; init; } = new Dictionary<string, string>();
}
