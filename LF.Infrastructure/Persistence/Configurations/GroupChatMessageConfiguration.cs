using LF.AppDomain.Entities.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class GroupChatMessageConfiguration : IEntityTypeConfiguration<GroupChatMessage>
{
    public void Configure(EntityTypeBuilder<GroupChatMessage> builder)
    {
        builder.ToTable("LFGroupChatMessages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();

        // Users live in a separate bounded context (LF.IdentityService); this is a plain scalar
        // reference by convention, not an EF navigation/FK, even though it's physically the same DB.
        builder.Property(m => m.AuthorUserId).IsRequired();
        builder.Property(m => m.Body).IsRequired().HasMaxLength(GroupChatMessage.MaxBodyLength);
        builder.Property(m => m.SentAt).IsRequired();
        builder.Property(m => m.IsDeleted).IsRequired();
        builder.Property(m => m.DeletedAt);
        builder.Property(m => m.DeletedByUserId);

        builder.HasOne<StudentGroup>()
            .WithMany()
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // History is keyset-paged by id within a group, and the unread count compares ids too.
        builder.HasIndex(m => new { m.GroupId, m.Id });
    }
}
