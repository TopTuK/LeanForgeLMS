using LF.Application.ModelDto.Enrollment;
using LF.CourseService;
using Mapster;

namespace LF.Infrastructure.Services.Enrollment;

// Same Mapster nested-list-of-list Timestamp compile issue documented in CourseReplyMappingConfig
// applies here (EnrollmentDetailReply.Chapters[].Lessons[]) — explicit member mapping works around it.
internal sealed class EnrollmentReplyMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<EnrollmentDetailReply, EnrollmentDetailDto>()
            .Map(dest => dest.EnrolledAt, src => src.EnrolledAt.ToDateTime())
            .Map(dest => dest.CompletedAt, src => src.CompletedAt == null ? (DateTime?)null : src.CompletedAt.ToDateTime());

        config.NewConfig<EnrollmentSummaryReply, EnrollmentSummaryDto>()
            .Map(dest => dest.EnrolledAt, src => src.EnrolledAt.ToDateTime())
            .Map(dest => dest.CompletedAt, src => src.CompletedAt == null ? (DateTime?)null : src.CompletedAt.ToDateTime());
    }
}
