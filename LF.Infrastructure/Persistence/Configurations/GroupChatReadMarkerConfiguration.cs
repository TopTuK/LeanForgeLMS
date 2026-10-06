using LF.AppDomain.Entities.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class GroupChatReadMarkerConfiguration : IEntityTypeConfiguration<GroupChatReadMarker>
{
    public void Configure(EntityTypeBuilder<GroupChatReadMarker> builder)
    {
        builder.ToTable("LFGroupChatReadMarkers");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();

        builder.Property(m => m.UserId).IsRequired();
        builder.Property(m => m.LastSeenMessageId).IsRequired();

        builder.HasOne<StudentGroup>()
            .WithMany()
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.GroupId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId);
    }
}
