using Grpc.Core;
using LF.Application.ModelDto.Promo;
using LF.Application.Services.Promo;
using LF.CourseService;
using Mapster;
using Microsoft.Extensions.Logging;

namespace LF.Infrastructure.Services.Promo;

internal sealed class GrpcPromoCodeService(ILogger<GrpcPromoCodeService> logger,
    CourseServiceRpc.CourseServiceRpcClient courseServiceRpcClient) : IGrpcPromoCodeService
{
    private readonly ILogger<GrpcPromoCodeService> _logger = logger;
    private readonly CourseServiceRpc.CourseServiceRpcClient _courseServiceRpcClient = courseServiceRpcClient;

    public async Task<PromoCodeDto> CreatePromoCodeAsync(CreatePromoCodeDto dto, int createdByUserId)
    {
        _logger.LogInformation("GrpcPromoCodeService::CreatePromoCodeAsync: called with Code={Code} CreatedByUserId={CreatedByUserId}", dto.Code, createdByUserId);

        var request = dto.Adapt<CreatePromoCodeRequest>();
        request.CreatedByUserId = createdByUserId;

        try
        {
            var reply = await _courseServiceRpcClient.CreatePromoCodeAsync(request);
            return reply.Adapt<PromoCodeDto>();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            throw new ArgumentException(ex.Status.Detail);
        }
    }

    public async Task<PagedPromoCodesDto> ListPromoCodesAsync(int page, int pageSize)
    {
        _logger.LogInformation("GrpcPromoCodeService::ListPromoCodesAsync: called with Page={Page} PageSize={PageSize}", page, pageSize);

        var reply = await _courseServiceRpcClient.ListPromoCodesAsync(new ListPromoCodesRequest { Page = page, PageSize = pageSize });
        return new PagedPromoCodesDto { Items = reply.Items.Adapt<List<PromoCodeDto>>(), TotalCount = reply.TotalCount };
    }

    public async Task<bool> DeactivatePromoCodeAsync(int id)
    {
        _logger.LogInformation("GrpcPromoCodeService::DeactivatePromoCodeAsync: called with Id={PromoCodeId}", id);

        var reply = await _courseServiceRpcClient.DeactivatePromoCodeAsync(new DeactivatePromoCodeRequest { Id = id });
        return reply.Deactivated;
    }
}
