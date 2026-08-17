using LF.AppDomain.Entities.Course;
using LF.Application.ModelDto.Course;
using Mapster;

namespace LF.Application.Common.Mapping;

internal sealed class CourseMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Category, CategoryDto>();

        config.NewConfig<LessonPart, LessonPartDto>()
            .Map(dest => dest.StorageObjectKey, src => src.StorageObject != null ? src.StorageObject.ObjectKey : null)
            .Map(dest => dest.StorageObjectContentType, src => src.StorageObject != null ? src.StorageObject.ContentType : null);

        config.NewConfig<Lesson, LessonDto>()
            .Map(dest => dest.Parts, src => src.Parts.OrderBy(p => p.SortOrder));

        config.NewConfig<Chapter, ChapterDto>()
            .Map(dest => dest.Lessons, src => src.Lessons.OrderBy(l => l.SortOrder));

        config.NewConfig<Course, CourseDetailDto>()
            .Map(dest => dest.CategoryName, src => src.Category.Name)
            .Map(dest => dest.CoverImageKey, src => src.CoverImageStorageObject != null ? src.CoverImageStorageObject.ObjectKey : null)
            .Map(dest => dest.CoverImageContentType, src => src.CoverImageStorageObject != null ? src.CoverImageStorageObject.ContentType : null)
            .Map(dest => dest.Chapters, src => src.Chapters.OrderBy(c => c.SortOrder));

        config.NewConfig<Course, CourseSummaryDto>()
            .Map(dest => dest.CategoryName, src => src.Category.Name)
            .Map(dest => dest.CoverImageKey, src => src.CoverImageStorageObject != null ? src.CoverImageStorageObject.ObjectKey : null)
            .Map(dest => dest.CoverImageContentType, src => src.CoverImageStorageObject != null ? src.CoverImageStorageObject.ContentType : null)
            .Map(dest => dest.ChapterCount, src => src.Chapters.Count);
    }
}
