using LF.Application.ModelDto.Promo;

namespace LF.Application.Services.PromoCodeAdmin;

public interface IPromoCodeAdminService
{
    Task<PromoCodeDto> CreateAsync(CreatePromoCodeDto dto, int createdByUserId);
    Task<PagedPromoCodesDto> ListAsync(int page, int pageSize);
    Task<bool> DeactivateAsync(int id);
}
