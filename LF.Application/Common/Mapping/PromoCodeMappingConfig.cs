using LF.AppDomain.Entities.Course;
using LF.Application.ModelDto.Promo;
using Mapster;

namespace LF.Application.Common.Mapping;

internal sealed class PromoCodeMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PromoCode, PromoCodeDto>()
            .Map(dest => dest.CourseTitle, src => src.Course != null ? src.Course.Title : null);
    }
}
