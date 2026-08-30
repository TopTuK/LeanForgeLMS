using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using LF.Application.ModelDto.Promo;
using LF.CourseService;
using Mapster;

namespace LF.Infrastructure.Services.Promo;

// Promo discount amounts and prices cross the wire as invariant-culture strings (proto has no decimal).
internal sealed class PromoCodeReplyMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreatePromoCodeDto, CreatePromoCodeRequest>()
            .Map(dest => dest.DiscountValue, src => src.DiscountValue.ToString(CultureInfo.InvariantCulture))
            .Map(dest => dest.CourseId, src => src.CourseId)
            .Map(dest => dest.MaxRedemptions, src => src.MaxRedemptions)
            .Map(dest => dest.ExpiresAt,
                src => src.ExpiresAt == null
                    ? (Timestamp?)null
                    : Timestamp.FromDateTime(DateTime.SpecifyKind(src.ExpiresAt.Value, DateTimeKind.Utc)));

        config.NewConfig<PromoCodeReply, PromoCodeDto>()
            .Map(dest => dest.DiscountValue, src => decimal.Parse(src.DiscountValue, CultureInfo.InvariantCulture))
            .Map(dest => dest.CourseId, src => src.CourseId)
            .Map(dest => dest.CourseTitle, src => src.CourseTitle)
            .Map(dest => dest.MaxRedemptions, src => src.MaxRedemptions)
            .Map(dest => dest.ExpiresAt, src => src.ExpiresAt == null ? (DateTime?)null : src.ExpiresAt.ToDateTime())
            .Map(dest => dest.CreatedAt, src => src.CreatedAt.ToDateTime());

        config.NewConfig<PromoCodeValidationReply, PromoCodeValidationDto>()
            .Map(dest => dest.IsValid, src => src.IsValid)
            .Map(dest => dest.Reason, src => src.Reason)
            .Map(dest => dest.OriginalPrice, src => ParseOrZero(src.OriginalPrice))
            .Map(dest => dest.DiscountedPrice, src => ParseOrZero(src.DiscountedPrice))
            .Map(dest => dest.DiscountAmount, src => ParseOrZero(src.DiscountAmount));
    }

    private static decimal ParseOrZero(string? text) =>
        string.IsNullOrEmpty(text) ? 0m : decimal.Parse(text, CultureInfo.InvariantCulture);
}
