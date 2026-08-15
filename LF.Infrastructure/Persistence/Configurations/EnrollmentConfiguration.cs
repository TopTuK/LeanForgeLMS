using LF.AppDomain.Entities.Course;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("LFEnrollments");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Users live in a separate bounded context (LF.IdentityService); this is a plain scalar
        // reference by convention, not an EF navigation/FK, even though it's physically the same DB.
        builder.Property(e => e.UserId).IsRequired();

        builder.Property(e => e.CompletedLessonIds).HasColumnType("integer[]");

        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.CourseId, e.UserId }).IsUnique();
    }
}
