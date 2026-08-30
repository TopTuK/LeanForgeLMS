namespace LF.Application.ModelDto.Promo;

public sealed class PromoCodeValidationDto
{
    public bool IsValid { get; init; }
    public string? Reason { get; init; }
    public decimal OriginalPrice { get; init; }
    public decimal DiscountedPrice { get; init; }
    public decimal DiscountAmount { get; init; }
}
