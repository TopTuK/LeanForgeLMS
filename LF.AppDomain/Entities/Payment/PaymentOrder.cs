using LF.AppDomain.Models.Payment.Enums;

namespace LF.AppDomain.Entities.Payment;

public sealed class PaymentOrder
{
    public const string RobokassaProvider = "Robokassa";
    public const int MaxDescriptionLength = 100;

    private PaymentOrder()
    {
    }

    public int Id { get; private set; }
    public int EnrollmentId { get; private set; }
    public int UserId { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = null!;
    public string Provider { get; private set; } = null!;
    public PaymentOrderStatus Status { get; private set; }
    public string? ProviderOperationId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }

    public bool IsOpen => Status == PaymentOrderStatus.Pending;

    public static PaymentOrder Create(
        int enrollmentId,
        int userId,
        decimal amount,
        string description,
        DateTime createdAt,
        string provider = RobokassaProvider)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(enrollmentId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(userId, 0);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Payment order description cannot be empty.", nameof(description));

        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Payment provider cannot be empty.", nameof(provider));

        var trimmed = description.Trim();
        if (trimmed.Length > MaxDescriptionLength)
            trimmed = trimmed[..MaxDescriptionLength];

        return new PaymentOrder
        {
            EnrollmentId = enrollmentId,
            UserId = userId,
            Amount = decimal.Round(amount, 2),
            Description = trimmed,
            Provider = provider,
            Status = PaymentOrderStatus.Pending,
            CreatedAt = createdAt,
        };
    }

    /// <returns>true if this call settled the order; false if it was already paid (idempotent replay).</returns>
    public bool MarkPaid(decimal receivedAmount, DateTime nowUtc, string? providerOperationId = null)
    {
        if (Status == PaymentOrderStatus.Paid)
            return false;

        if (Status != PaymentOrderStatus.Pending)
            throw new InvalidOperationException($"Cannot settle a payment order in status {Status}.");

        if (decimal.Round(receivedAmount, 2) != Amount)
            throw new InvalidOperationException(
                $"Received amount {receivedAmount} does not match the order amount {Amount}.");

        Status = PaymentOrderStatus.Paid;
        PaidAt = nowUtc;
        ProviderOperationId = providerOperationId;
        return true;
    }

    public void MarkFailed()
    {
        if (Status != PaymentOrderStatus.Pending)
            throw new InvalidOperationException($"Cannot fail a payment order in status {Status}.");

        Status = PaymentOrderStatus.Failed;
    }

    public void MarkCancelled()
    {
        if (Status != PaymentOrderStatus.Pending)
            throw new InvalidOperationException($"Cannot cancel a payment order in status {Status}.");

        Status = PaymentOrderStatus.Cancelled;
    }
}
