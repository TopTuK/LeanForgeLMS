namespace LF.Application.Common.Exceptions;

// IsTransient decides whether the outbox retries (network blip, 4xx) or gives up (5xx mailbox rejected).
public sealed class EmailDeliveryException(string message, bool isTransient, Exception? innerException = null)
    : Exception(message, innerException)
{
    public bool IsTransient { get; } = isTransient;
}
