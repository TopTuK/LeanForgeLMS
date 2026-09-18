using LF.AppDomain.Entities.Qna;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class LessonQuestionMessageConfiguration : IEntityTypeConfiguration<LessonQuestionMessage>
{
    public void Configure(EntityTypeBuilder<LessonQuestionMessage> builder)
    {
        builder.ToTable("LFLessonQuestionMessages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();

        builder.Property(m => m.AuthorUserId).IsRequired();
        builder.Property(m => m.AuthorRole).IsRequired();
        builder.Property(m => m.Body).IsRequired().HasMaxLength(LessonQuestionMessage.MaxBodyLength);
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.IsDeleted).IsRequired();
        builder.Property(m => m.DeletedAt);
        builder.Property(m => m.DeletedByUserId);

        builder.HasIndex(m => new { m.LessonQuestionId, m.CreatedAt });
    }
}
