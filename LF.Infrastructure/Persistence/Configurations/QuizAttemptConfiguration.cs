using LF.AppDomain.Entities.Course;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.ToTable("LFQuizAttempts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        // No FK to a specific LessonPart row: Lesson.ReplaceParts clears and rebuilds every
        // LessonPart (fresh ids) on each author save, so attempt history is keyed by the
        // stable LessonId instead — the same loose convention Enrollment.CompletedLessonIds
        // already uses, rather than a FK that would cascade-delete grading history on unrelated edits.
        builder.Property(a => a.LessonId).IsRequired();

        builder.Property(a => a.ScorePercent).IsRequired();
        builder.Property(a => a.Passed).IsRequired();
        builder.Property(a => a.SubmittedAtUtc).IsRequired();

        builder.HasOne<Enrollment>()
            .WithMany()
            .HasForeignKey(a => a.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Answers)
            .WithOne()
            .HasForeignKey(ans => ans.QuizAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(a => a.Answers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
