using LF.AppDomain.Entities.Course;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class QuizAnswerConfiguration : IEntityTypeConfiguration<QuizAnswer>
{
    public void Configure(EntityTypeBuilder<QuizAnswer> builder)
    {
        builder.ToTable("LFQuizAnswers");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        // No FK to QuizQuestion — questions live on the LessonPart, which is fully replaced on
        // every author save (see QuizAttemptConfiguration); plain int keeps this consistent.
        builder.Property(a => a.QuestionId).IsRequired();
        builder.Property(a => a.SelectedOptionIds).HasColumnType("integer[]");
    }
}
