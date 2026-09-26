using System.Diagnostics.CodeAnalysis;

namespace LF.Infrastructure.Services.Email;

internal sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;

    // StartTls | SslOnConnect | Auto | None — maps to MailKit's SecureSocketOptions.
    public string Security { get; set; } = "StartTls";

    // Credentials are never committed: user-secrets in dev, Smtp__UserName/Smtp__Password via .env in Docker.
    public string? UserName { get; set; }
    public string? Password { get; set; }

    public string? FromAddress { get; set; }
    public string FromName { get; set; } = "LeanForge";

    public int TimeoutSeconds { get; set; } = 30;

    private const string Placeholder = "CHANGE_ME";

    [MemberNotNullWhen(true, nameof(Host), nameof(FromAddress))]
    public bool IsConfigured =>
        IsSet(Host)
        && IsSet(FromAddress)
        && Port > 0;

    // Anonymous relays are allowed. Only authenticate when a real login was supplied.
    [MemberNotNullWhen(true, nameof(UserName), nameof(Password))]
    public bool HasCredentials => IsSet(UserName) && IsSet(Password);

    private static bool IsSet([NotNullWhen(true)] string? value) =>
        !string.IsNullOrWhiteSpace(value) && !string.Equals(value, Placeholder, StringComparison.Ordinal);
}
