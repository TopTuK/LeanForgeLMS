using LF.AppDomain.Entities.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class LectureGroupConfiguration : IEntityTypeConfiguration<LectureGroup>
{
    public void Configure(EntityTypeBuilder<LectureGroup> builder)
    {
        builder.ToTable("LFLectureGroups");

        builder.HasKey(g => new { g.LectureId, g.GroupId });

        // Deleting a group drops it from its lectures; a lecture left with no groups simply stops
        // appearing in anyone's student schedule but stays visible to staff.
        builder.HasOne<StudentGroup>()
            .WithMany()
            .HasForeignKey(g => g.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(g => g.GroupId);
    }
}
