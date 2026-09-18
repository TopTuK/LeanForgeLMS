using LF.AppDomain.Entities.Course;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class CourseInstructorConfiguration : IEntityTypeConfiguration<CourseInstructor>
{
    public void Configure(EntityTypeBuilder<CourseInstructor> builder)
    {
        builder.ToTable("LFCourseInstructors");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();

        // Users live in a separate bounded context (LF.IdentityService); this is a plain scalar
        // reference by convention, not an EF navigation/FK, even though it's physically the same DB.
        builder.Property(i => i.UserId).IsRequired();
        builder.Property(i => i.AssignedByUserId).IsRequired();
        builder.Property(i => i.AssignedAt).IsRequired();

        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(i => i.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.CourseId, i.UserId }).IsUnique();

        // Answers "which courses does this user teach", the predicate behind the staff inbox.
        builder.HasIndex(i => i.UserId);
    }
}
