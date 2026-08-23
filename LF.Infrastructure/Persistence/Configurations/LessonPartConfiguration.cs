using LF.AppDomain.Entities.Course;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class LessonPartConfiguration : IEntityTypeConfiguration<LessonPart>
{
    public void Configure(EntityTypeBuilder<LessonPart> builder)
    {
        builder.ToTable("LFLessonParts");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.PartType).IsRequired();
        builder.Property(p => p.SortOrder).IsRequired();
        builder.Property(p => p.Html);
        builder.Property(p => p.QuizPassThresholdPercent);

        builder.HasOne(p => p.StorageObject)
            .WithMany()
            .HasForeignKey(p => p.StorageObjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.QuizQuestions)
            .WithOne()
            .HasForeignKey(q => q.LessonPartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.QuizQuestions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
