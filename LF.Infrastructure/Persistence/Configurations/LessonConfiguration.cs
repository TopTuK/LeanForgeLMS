using LF.AppDomain.Entities.Course;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("LFLessons");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedOnAdd();

        builder.Property(l => l.Title).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Content).IsRequired();

        builder.HasMany(l => l.Parts)
            .WithOne()
            .HasForeignKey(p => p.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.Parts).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
