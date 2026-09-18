using LF.AppDomain.Entities.Course;
using LF.AppDomain.Entities.Qna;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class LessonQuestionConfiguration : IEntityTypeConfiguration<LessonQuestion>
{
    public void Configure(EntityTypeBuilder<LessonQuestion> builder)
    {
        builder.ToTable("LFLessonQuestions");

        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).ValueGeneratedOnAdd();

        builder.Property(q => q.Title).IsRequired().HasMaxLength(LessonQuestion.MaxTitleLength);
        builder.Property(q => q.Status).IsRequired();
        builder.Property(q => q.CreatedAt).IsRequired();
        builder.Property(q => q.LastMessageAt).IsRequired();
        builder.Property(q => q.LastMessageAuthorUserId).IsRequired();

        // Users live in a separate bounded context (LF.IdentityService); this is a plain scalar
        // reference by convention, not an EF navigation/FK, even though it's physically the same DB.
        builder.Property(q => q.StudentUserId).IsRequired();

        // Both FKs cascade. That leaves two delete paths to this table when a course goes away
        // (directly, and via Chapter -> Lesson), which Postgres handles fine — unlike SQL Server it
        // does not reject multiple cascade paths.
        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(q => q.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(q => q.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Messages)
            .WithOne()
            .HasForeignKey(m => m.LessonQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(q => q.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);

        // The two inbox queries: staff list by course, student list by author. Both page on
        // LastMessageAt descending.
        builder.HasIndex(q => new { q.CourseId, q.LastMessageAt });
        builder.HasIndex(q => new { q.StudentUserId, q.LastMessageAt });
        builder.HasIndex(q => q.LessonId);
    }
}
