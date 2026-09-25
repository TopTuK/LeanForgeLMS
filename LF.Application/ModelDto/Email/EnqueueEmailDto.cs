namespace LF.Application.ModelDto.Email;

public sealed class EnqueueEmailDto
{
    public string ToAddress { get; init; } = null!;
    public string? ToName { get; init; }
    public string Subject { get; init; } = null!;
    public string HtmlBody { get; init; } = null!;
    public string? TextBody { get; init; }

    // Earliest delivery time (UTC). Null sends on the next dispatcher run.
    public DateTime? NotBefore { get; init; }
}
