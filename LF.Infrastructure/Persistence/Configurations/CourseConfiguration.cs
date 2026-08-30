using LF.AppDomain.Entities.Course;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("LFCourses");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
        builder.Property(c => c.ShortIntroduction).IsRequired().HasMaxLength(500);
        builder.Property(c => c.Description).IsRequired();
        builder.Property(c => c.CoverType).IsRequired();
        builder.Property(c => c.CoverColor);

        builder.Property(c => c.PricingType).IsRequired();
        builder.Property(c => c.Price).HasColumnType("numeric(12,2)");
        builder.Property(c => c.EnrollmentMode).IsRequired();

        // Users live in a separate bounded context (LF.IdentityService); this is a plain scalar
        // reference by convention, not an EF navigation/FK, even though it's physically the same DB.
        builder.Property(c => c.CreatedByUserId).IsRequired();

        builder.HasOne(c => c.Category)
            .WithMany()
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.CoverImageStorageObject)
            .WithMany()
            .HasForeignKey(c => c.CoverImageStorageObjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Chapters)
            .WithOne()
            .HasForeignKey(ch => ch.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Chapters).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
