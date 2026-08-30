using System.Security.Claims;
using LF.AppDomain.Models.Course.Enums;
using LF.Application.ModelDto.Promo;
using LF.Application.Services.PromoCodeAdmin;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

public sealed class AdminPromoCodeEndpoints : IEndpointGroup
{
    private const int DefaultPageSize = 20;

    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/promo-codes").WithTags("AdminPromoCodes").RequireAuthorization("AdminOnly");

        group.MapGet("/", async Task<Ok<PagedPromoCodesResponse>>
            (int? page, int? pageSize, IPromoCodeAdminService promoCodeService, CancellationToken ct) =>
        {
            var effectivePage = page is > 0 ? page.Value : 1;
            var effectivePageSize = pageSize is > 0 ? pageSize.Value : DefaultPageSize;

            var result = await promoCodeService.ListAsync(effectivePage, effectivePageSize);
            return TypedResults.Ok(new PagedPromoCodesResponse(
                [.. result.Items.Select(ToResponse)], result.TotalCount, effectivePage, effectivePageSize));
        });

        group.MapPost("/", async Task<Results<Created<PromoCodeResponse>, UnauthorizedHttpResult, ValidationProblem>>
            (CreatePromoCodeRequest request, ClaimsPrincipal user, IPromoCodeAdminService promoCodeService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new CreatePromoCodeRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            var dto = new CreatePromoCodeDto
            {
                Code = request.Code,
                DiscountType = Enum.Parse<PromoCodeDiscountType>(request.DiscountType, ignoreCase: true),
                DiscountValue = request.DiscountValue,
                CourseId = request.CourseId,
                ExpiresAt = request.ExpiresAt,
                MaxRedemptions = request.MaxRedemptions,
            };

            try
            {
                var promoCode = await promoCodeService.CreateAsync(dto, userId.Value);
                return TypedResults.Created($"/api/admin/promo-codes/{promoCode.Id}", ToResponse(promoCode));
            }
            catch (ArgumentException ex)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["code"] = [ex.Message] });
            }
        });

        group.MapPost("/{id:int}/deactivate", async Task<Results<NoContent, NotFound>>
            (int id, IPromoCodeAdminService promoCodeService, CancellationToken ct) =>
        {
            var deactivated = await promoCodeService.DeactivateAsync(id);
            return deactivated ? TypedResults.NoContent() : TypedResults.NotFound();
        });
    }

    private static PromoCodeResponse ToResponse(PromoCodeDto dto) => new(
        dto.Id,
        dto.Code,
        dto.DiscountType.ToString(),
        dto.DiscountValue,
        dto.CourseId,
        dto.CourseTitle,
        dto.ExpiresAt,
        dto.MaxRedemptions,
        dto.RedemptionCount,
        dto.IsActive,
        dto.CreatedAt);
}
