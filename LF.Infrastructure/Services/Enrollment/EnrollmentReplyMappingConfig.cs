using LF.Application.ModelDto.Enrollment;
using LF.CourseService;
using LF.Infrastructure.Services.Course;
using Mapster;

namespace LF.Infrastructure.Services.Enrollment;

// Same Mapster nested-list-of-list Timestamp compile issue documented in CourseReplyMappingConfig
// applies here (EnrollmentDetailReply.Chapters[].Lessons[]) — explicit member mapping works around it.
// Ruble amounts cross the wire as invariant-culture strings (proto has no decimal).
internal sealed class EnrollmentReplyMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<EnrollmentDetailReply, EnrollmentDetailDto>()
            .Map(dest => dest.EnrolledAt, src => src.EnrolledAt.ToDateTime())
            .Map(dest => dest.CompletedAt, src => src.CompletedAt == null ? (DateTime?)null : src.CompletedAt.ToDateTime())
            .Map(dest => dest.PricePaid, src => CourseReplyMappingConfig.ToPriceValue(src.PricePaid));

        config.NewConfig<EnrollmentSummaryReply, EnrollmentSummaryDto>()
            .Map(dest => dest.EnrolledAt, src => src.EnrolledAt.ToDateTime())
            .Map(dest => dest.CompletedAt, src => src.CompletedAt == null ? (DateTime?)null : src.CompletedAt.ToDateTime())
            .Map(dest => dest.PricePaid, src => CourseReplyMappingConfig.ToPriceValue(src.PricePaid));

        config.NewConfig<CourseCatalogItemReply, CourseCatalogItemDto>()
            .Map(dest => dest.Price, src => CourseReplyMappingConfig.ToPrice(src.PriceRub));

        config.NewConfig<CoursePreviewReply, CoursePreviewDto>()
            .Map(dest => dest.Price, src => CourseReplyMappingConfig.ToPrice(src.PriceRub));
    }
}
