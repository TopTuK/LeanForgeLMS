using LF.AppDomain.Entities.Qna;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class LessonQuestionReadMarkerConfiguration : IEntityTypeConfiguration<LessonQuestionReadMarker>
{
    public void Configure(EntityTypeBuilder<LessonQuestionReadMarker> builder)
    {
        builder.ToTable("LFLessonQuestionReadMarkers");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();

        // Users live in a separate bounded context (LF.IdentityService); this is a plain scalar
        // reference by convention, not an EF navigation/FK, even though it's physically the same DB.
        builder.Property(m => m.UserId).IsRequired();
        builder.Property(m => m.LastSeenAt).IsRequired();

        builder.HasOne<LessonQuestion>()
            .WithMany()
            .HasForeignKey(m => m.LessonQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Backs both the "have I read this thread" lookup and the unread-count anti-join.
        builder.HasIndex(m => new { m.LessonQuestionId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId);
    }
}
