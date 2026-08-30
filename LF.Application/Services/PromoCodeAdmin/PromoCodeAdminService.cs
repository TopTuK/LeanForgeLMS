using LF.Application.ModelDto.Promo;
using LF.Application.Services.Promo;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.PromoCodeAdmin;

internal sealed class PromoCodeAdminService(ILogger<PromoCodeAdminService> logger, IGrpcPromoCodeService grpcPromoCodeService) : IPromoCodeAdminService
{
    private readonly ILogger<PromoCodeAdminService> _logger = logger;
    private readonly IGrpcPromoCodeService _grpcPromoCodeService = grpcPromoCodeService;

    public async Task<PromoCodeDto> CreateAsync(CreatePromoCodeDto dto, int createdByUserId)
    {
        _logger.LogInformation("PromoCodeAdminService::CreateAsync: called with Code={Code} CreatedByUserId={CreatedByUserId}", dto.Code, createdByUserId);

        return await _grpcPromoCodeService.CreatePromoCodeAsync(dto, createdByUserId);
    }

    public async Task<PagedPromoCodesDto> ListAsync(int page, int pageSize)
    {
        _logger.LogInformation("PromoCodeAdminService::ListAsync: called with Page={Page} PageSize={PageSize}", page, pageSize);

        return await _grpcPromoCodeService.ListPromoCodesAsync(page, pageSize);
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        _logger.LogInformation("PromoCodeAdminService::DeactivateAsync: called with Id={PromoCodeId}", id);

        return await _grpcPromoCodeService.DeactivatePromoCodeAsync(id);
    }
}
