using LF.Application.ModelDto.Teaching;

namespace LF.WebApi.Endpoints;

public sealed record TeachingCourseResponse(
    int Id,
    string Title,
    string ShortIntroduction,
    string CategoryName,
    bool IsPublished,
    string CoverType,
    string? CoverColor,
    bool CanEditContent);

public static class TeachingCourseResponseMapper
{
    public static TeachingCourseResponse ToResponse(TeachingCourseDto dto) =>
        new(dto.Id, dto.Title, dto.ShortIntroduction, dto.CategoryName, dto.IsPublished,
            dto.CoverType.ToString(), dto.CoverColor?.ToString(), dto.CanEditContent);
}
