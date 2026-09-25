using System.Net.Sockets;
using LF.Application.Common.Exceptions;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Email;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LF.Infrastructure.Services.Email;

internal sealed class MailKitEmailSender(ILogger<MailKitEmailSender> logger, IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly ILogger<MailKitEmailSender> _logger = logger;
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            throw new EmailDeliveryException("SMTP is not configured (Smtp:Host / Smtp:FromAddress).", isTransient: true);

        var message = BuildMessage(email, _options.FromAddress);

        using var client = new SmtpClient { Timeout = (int)TimeSpan.FromSeconds(_options.TimeoutSeconds).TotalMilliseconds };

        try
        {
            await client.ConnectAsync(_options.Host, _options.Port, ParseSecurity(_options.Security), cancellationToken);

            if (_options.HasCredentials)
                await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);

            var response = await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            _logger.LogDebug("MailKitEmailSender::SendAsync: server accepted message {MessageId}: {Response}", message.MessageId, response);
        }
        catch (SmtpCommandException ex)
        {
            // 4xx replies are the server asking us to retry later. 5xx replies (unknown mailbox,
            // rejected content) will fail the same way every time.
            var isTransient = (int)ex.StatusCode is >= 400 and < 500;
            throw new EmailDeliveryException($"SMTP {(int)ex.StatusCode} {ex.ErrorCode}: {ex.Message}", isTransient, ex);
        }
        catch (Exception ex) when (ex is MailKit.Security.AuthenticationException or System.Security.Authentication.AuthenticationException or SslHandshakeException)
        {
            // Bad login or TLS failure. Kept transient: fixing the config should let queued mail go out
            // without re-enqueueing it, and MaxAttempts still bounds the retries.
            throw new EmailDeliveryException($"SMTP authentication/TLS failed: {ex.Message}", isTransient: true, ex);
        }
        catch (Exception ex) when (ex is SmtpProtocolException or ServiceNotConnectedException or IOException or SocketException or TimeoutException)
        {
            throw new EmailDeliveryException($"SMTP connection failed: {ex.Message}", isTransient: true, ex);
        }
    }

    private MimeMessage BuildMessage(OutgoingEmail email, string fromAddress)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, fromAddress));

        try
        {
            message.To.Add(new MailboxAddress(email.ToName, email.ToAddress));
        }
        catch (ParseException ex)
        {
            throw new EmailDeliveryException($"Invalid recipient address '{email.ToAddress}': {ex.Message}", isTransient: false, ex);
        }

        message.Subject = email.Subject;
        message.Body = new BodyBuilder { HtmlBody = email.HtmlBody, TextBody = email.TextBody }.ToMessageBody();

        return message;
    }

    private static SecureSocketOptions ParseSecurity(string value) => value.ToLowerInvariant() switch
    {
        "starttls" => SecureSocketOptions.StartTls,
        "sslonconnect" or "ssl" => SecureSocketOptions.SslOnConnect,
        "none" => SecureSocketOptions.None,
        _ => SecureSocketOptions.Auto,
    };
}
