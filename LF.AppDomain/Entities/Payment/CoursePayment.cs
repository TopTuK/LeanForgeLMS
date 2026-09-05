namespace LF.AppDomain.Entities.Payment;

// Denormalized, append-only record of a settled course payment, kept for marketing reporting.
// Independent of PaymentOrder/Enrollment/Course/User lifecycles — the snapshot survives their edits
// or deletion. Written when the Robokassa webhook confirms a payment and reconciled from settled
// PaymentOrders as a safety net.
public sealed class CoursePayment
{
    public const int MaxEmailLength = 256;
    public const int MaxNameLength = 256;
    public const int MaxCourseTitleLength = 200;
    public const int MaxProviderLength = 32;
    public const int MaxProviderOperationIdLength = 128;
    public const int MaxPromoCodeLength = 64;

    private CoursePayment()
    {
    }

    public int Id { get; private set; }
    public int PaymentOrderId { get; private set; }
    public int EnrollmentId { get; private set; }
    public int UserId { get; private set; }
    public string StudentEmail { get; private set; } = null!;
    public string StudentName { get; private set; } = null!;
    public int CourseId { get; private set; }
    public string CourseTitle { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string? PromoCode { get; private set; }
    public string Provider { get; private set; } = null!;
    public string? ProviderOperationId { get; private set; }
    public DateTime PaidAt { get; private set; }
    public DateTime RecordedAt { get; private set; }

    public static CoursePayment Record(
        int paymentOrderId,
        int enrollmentId,
        int courseId,
        int userId,
        string studentEmail,
        string studentName,
        string courseTitle,
        decimal amount,
        string? promoCode,
        string provider,
        string? providerOperationId,
        DateTime paidAt,
        DateTime recordedAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(paymentOrderId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(enrollmentId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(courseId, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(userId, 0);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        if (string.IsNullOrWhiteSpace(studentEmail))
            throw new ArgumentException("Student email cannot be empty.", nameof(studentEmail));

        if (string.IsNullOrWhiteSpace(courseTitle))
            throw new ArgumentException("Course title cannot be empty.", nameof(courseTitle));

        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Payment provider cannot be empty.", nameof(provider));

        return new CoursePayment
        {
            PaymentOrderId = paymentOrderId,
            EnrollmentId = enrollmentId,
            CourseId = courseId,
            UserId = userId,
            StudentEmail = Clamp(studentEmail.Trim(), MaxEmailLength),
            StudentName = Clamp(studentName.Trim(), MaxNameLength),
            CourseTitle = Clamp(courseTitle.Trim(), MaxCourseTitleLength),
            Amount = decimal.Round(amount, 2),
            PromoCode = string.IsNullOrWhiteSpace(promoCode) ? null : Clamp(promoCode.Trim(), MaxPromoCodeLength),
            Provider = Clamp(provider.Trim(), MaxProviderLength),
            ProviderOperationId = string.IsNullOrWhiteSpace(providerOperationId)
                ? null
                : Clamp(providerOperationId.Trim(), MaxProviderOperationIdLength),
            PaidAt = paidAt,
            RecordedAt = recordedAt,
        };
    }

    private static string Clamp(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
