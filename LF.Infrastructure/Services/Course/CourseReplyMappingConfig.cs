using System.Globalization;
using LF.Application.ModelDto.Course;
using LF.CourseService;
using Mapster;

namespace LF.Infrastructure.Services.Course;

// Mapster's default dynamic adapter fails to compile google.protobuf.Timestamp -> DateTime when
// it's a member of a nested list-of-list projection (CourseDetailReply.Chapters[].Lessons[]),
// throwing "Cannot convert immutable type" — unlike the flat GetUserReply -> UserDto case, which
// has no nested collections and compiles fine via the default adapter. Explicit member mapping
// works around it. The ruble price crosses the wire as an invariant-culture string (proto has no
// decimal), so it also needs explicit conversion here.
internal sealed class CourseReplyMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateCourseDto, CreateCourseRequest>()
            .Map(dest => dest.PriceRub, src => ToText(src.Price));

        config.NewConfig<CourseDetailReply, CourseDetailDto>()
            .Map(dest => dest.CreatedAt, src => src.CreatedAt.ToDateTime())
            .Map(dest => dest.Price, src => ToPrice(src.PriceRub));

        config.NewConfig<CourseSummaryReply, CourseSummaryDto>()
            .Map(dest => dest.CreatedAt, src => src.CreatedAt.ToDateTime())
            .Map(dest => dest.Price, src => ToPrice(src.PriceRub));
    }

    internal static string? ToText(decimal? value) =>
        value?.ToString(CultureInfo.InvariantCulture);

    internal static decimal? ToPrice(string? text) =>
        string.IsNullOrEmpty(text) ? null : decimal.Parse(text, CultureInfo.InvariantCulture);

    internal static decimal ToPriceValue(string? text) =>
        string.IsNullOrEmpty(text) ? 0m : decimal.Parse(text, CultureInfo.InvariantCulture);
}
