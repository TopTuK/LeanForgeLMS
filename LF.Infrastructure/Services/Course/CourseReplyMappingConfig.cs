using LF.Application.ModelDto.Course;
using LF.CourseService;
using Mapster;

namespace LF.Infrastructure.Services.Course;

// Mapster's default dynamic adapter fails to compile google.protobuf.Timestamp -> DateTime when
// it's a member of a nested list-of-list projection (CourseDetailReply.Chapters[].Lessons[]),
// throwing "Cannot convert immutable type" — unlike the flat GetUserReply -> UserDto case, which
// has no nested collections and compiles fine via the default adapter. Explicit member mapping
// works around it.
internal sealed class CourseReplyMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CourseDetailReply, CourseDetailDto>()
            .Map(dest => dest.CreatedAt, src => src.CreatedAt.ToDateTime());

        config.NewConfig<CourseSummaryReply, CourseSummaryDto>()
            .Map(dest => dest.CreatedAt, src => src.CreatedAt.ToDateTime());
    }
}
