using LF.AppDomain.Entities.Course;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class CourseContentChangeConfiguration : IEntityTypeConfiguration<CourseContentChange>
{
    public void Configure(EntityTypeBuilder<CourseContentChange> builder)
    {
        builder.ToTable("LFCourseContentChanges");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.CourseId).IsRequired();
        builder.Property(c => c.Kind).IsRequired();

        // Deleting a lesson (or its course) drops its changes: nothing to announce about a lesson that is gone.
        builder.HasOne(c => c.Lesson)
            .WithMany()
            .HasForeignKey(c => c.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(c => c.IsPending);

        // Serves the digest job's "pending changes grouped by course" scan.
        builder.HasIndex(c => new { c.NotifiedAt, c.CourseId });
        builder.HasIndex(c => c.LessonId);
    }
}
