using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using LF.Application.ModelDto.Course;
using LF.Application.ModelDto.Enrollment;
using Mapster;

namespace LF.CourseService.Services;

// Mirrors the client-side fix in LF.Infrastructure.Services.Course.CourseReplyMappingConfig —
// Mapster's default dynamic adapter silently leaves Timestamp at its epoch default rather than
// converting from DateTime, so the DTO -> Reply direction also needs explicit member mapping.
// The ruble price/amount crosses the wire as an invariant-culture string (proto has no decimal).
internal sealed class CourseReplyMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateCourseRequest, CreateCourseDto>()
            .Map(dest => dest.Price, src => ToPrice(src.PriceRub));

        config.NewConfig<CourseDetailDto, CourseDetailReply>()
            .Map(dest => dest.CreatedAt, src => Timestamp.FromDateTime(DateTime.SpecifyKind(src.CreatedAt, DateTimeKind.Utc)))
            .Map(dest => dest.PriceRub, src => ToText(src.Price));

        config.NewConfig<CourseSummaryDto, CourseSummaryReply>()
            .Map(dest => dest.CreatedAt, src => Timestamp.FromDateTime(DateTime.SpecifyKind(src.CreatedAt, DateTimeKind.Utc)))
            .Map(dest => dest.PriceRub, src => ToText(src.Price));

        config.NewConfig<CourseCatalogItemDto, CourseCatalogItemReply>()
            .Map(dest => dest.PriceRub, src => ToText(src.Price));

        config.NewConfig<CoursePreviewDto, CoursePreviewReply>()
            .Map(dest => dest.PriceRub, src => ToText(src.Price));

        config.NewConfig<EnrollmentDetailDto, EnrollmentDetailReply>()
            .Map(dest => dest.PricePaid, src => src.PricePaid.ToString(CultureInfo.InvariantCulture));

        config.NewConfig<EnrollmentSummaryDto, EnrollmentSummaryReply>()
            .Map(dest => dest.PricePaid, src => src.PricePaid.ToString(CultureInfo.InvariantCulture));
    }

    internal static string? ToText(decimal? value) =>
        value?.ToString(CultureInfo.InvariantCulture);

    internal static decimal? ToPrice(string? text) =>
        string.IsNullOrEmpty(text) ? null : decimal.Parse(text, CultureInfo.InvariantCulture);
}
