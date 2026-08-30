namespace LF.Application.ModelDto.Promo;

public sealed class PagedPromoCodesDto
{
    public IReadOnlyList<PromoCodeDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
}
