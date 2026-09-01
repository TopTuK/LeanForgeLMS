namespace LF.Infrastructure.Services.Payment;

internal sealed class RobokassaOptions
{
    public const string SectionName = "Robokassa";

    public string MerchantLogin { get; set; } = null!;
    public string Password1 { get; set; } = null!;
    public string Password2 { get; set; } = null!;

    // MD5 | SHA256 | SHA512 — must match the algorithm set in the merchant cabinet.
    public string HashAlgorithm { get; set; } = "SHA256";

    public bool IsTest { get; set; }
    public string PaymentPageUrl { get; set; } = "https://auth.robokassa.ru/Merchant/Index.aspx";
    public string Culture { get; set; } = "ru";

    // Public LF.WebApi URLs Robokassa redirects the browser back to.
    public string SuccessUrl { get; set; } = null!;
    public string FailUrl { get; set; } = null!;

    public RobokassaFiscalizationOptions Fiscalization { get; set; } = new();
}

internal sealed class RobokassaFiscalizationOptions
{
    public bool Enabled { get; set; }
    public string Tax { get; set; } = "none";
    public string PaymentMethod { get; set; } = "full_payment";
    public string PaymentObject { get; set; } = "service";
}
