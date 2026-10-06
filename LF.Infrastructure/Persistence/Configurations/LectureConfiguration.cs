using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class LectureConfiguration : IEntityTypeConfiguration<Lecture>
{
    public void Configure(EntityTypeBuilder<Lecture> builder)
    {
        builder.ToTable("LFLectures");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedOnAdd();

        builder.Property(l => l.Title).IsRequired().HasMaxLength(Lecture.MaxTitleLength);
        builder.Property(l => l.Description).HasMaxLength(Lecture.MaxDescriptionLength);
        builder.Property(l => l.MeetingUrl).HasMaxLength(Lecture.MaxMeetingUrlLength);
        builder.Property(l => l.StartsAt).IsRequired();
        builder.Property(l => l.DurationMinutes).IsRequired();
        builder.Property(l => l.IsCancelled).IsRequired();
        builder.Property(l => l.CreatedByUserId).IsRequired();
        builder.Property(l => l.CreatedAt).IsRequired();

        builder.Ignore(l => l.EndsAt);

        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(l => l.Groups)
            .WithOne()
            .HasForeignKey(g => g.LectureId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.Groups).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(l => new { l.CourseId, l.StartsAt });
    }
}
