using Google.Protobuf.WellKnownTypes;
using LF.Application.ModelDto.Course;
using Mapster;

namespace LF.CourseService.Services;

// Mirrors the client-side fix in LF.Infrastructure.Services.Course.CourseReplyMappingConfig —
// Mapster's default dynamic adapter silently leaves Timestamp at its epoch default rather than
// converting from DateTime, so the DTO -> Reply direction also needs explicit member mapping.
internal sealed class CourseReplyMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CourseDetailDto, CourseDetailReply>()
            .Map(dest => dest.CreatedAt, src => Timestamp.FromDateTime(DateTime.SpecifyKind(src.CreatedAt, DateTimeKind.Utc)));

        config.NewConfig<CourseSummaryDto, CourseSummaryReply>()
            .Map(dest => dest.CreatedAt, src => Timestamp.FromDateTime(DateTime.SpecifyKind(src.CreatedAt, DateTimeKind.Utc)));
    }
}
