using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class StudentGroupConfiguration : IEntityTypeConfiguration<StudentGroup>
{
    public void Configure(EntityTypeBuilder<StudentGroup> builder)
    {
        builder.ToTable("LFStudentGroups");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedOnAdd();

        builder.Property(g => g.Name).IsRequired().HasMaxLength(StudentGroup.MaxNameLength);
        builder.Property(g => g.Description).HasMaxLength(StudentGroup.MaxDescriptionLength);
        builder.Property(g => g.CreatedByUserId).IsRequired();
        builder.Property(g => g.CreatedAt).IsRequired();

        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(g => g.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Members)
            .WithOne()
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(g => g.Members).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(g => new { g.CourseId, g.Name }).IsUnique();
    }
}
