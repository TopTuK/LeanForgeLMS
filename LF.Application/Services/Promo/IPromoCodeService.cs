using LF.Application.ModelDto.Promo;

namespace LF.Application.Services.Promo;

public interface IPromoCodeService
{
    Task<PromoCodeDto> CreatePromoCodeAsync(CreatePromoCodeDto dto, int createdByUserId);
    Task<PagedPromoCodesDto> ListPromoCodesAsync(int page, int pageSize);
    Task<bool> DeactivatePromoCodeAsync(int id);
}
