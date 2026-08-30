using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using LF.Application.ModelDto.Promo;
using Mapster;

namespace LF.CourseService.Services;

internal sealed class PromoCodeReplyMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PromoCodeDto, PromoCodeReply>()
            .Map(dest => dest.DiscountValue, src => src.DiscountValue.ToString(CultureInfo.InvariantCulture))
            .Map(dest => dest.CourseId, src => src.CourseId)
            .Map(dest => dest.CourseTitle, src => src.CourseTitle)
            .Map(dest => dest.MaxRedemptions, src => src.MaxRedemptions)
            .Map(dest => dest.ExpiresAt,
                src => src.ExpiresAt == null
                    ? (Timestamp?)null
                    : Timestamp.FromDateTime(DateTime.SpecifyKind(src.ExpiresAt.Value, DateTimeKind.Utc)))
            .Map(dest => dest.CreatedAt, src => Timestamp.FromDateTime(DateTime.SpecifyKind(src.CreatedAt, DateTimeKind.Utc)));
    }
}
