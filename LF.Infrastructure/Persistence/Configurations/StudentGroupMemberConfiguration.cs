using LF.AppDomain.Entities.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class StudentGroupMemberConfiguration : IEntityTypeConfiguration<StudentGroupMember>
{
    public void Configure(EntityTypeBuilder<StudentGroupMember> builder)
    {
        builder.ToTable("LFStudentGroupMembers");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();

        // Users live in a separate bounded context (LF.IdentityService); this is a plain scalar
        // reference by convention, not an EF navigation/FK, even though it's physically the same DB.
        builder.Property(m => m.UserId).IsRequired();
        builder.Property(m => m.AddedByUserId).IsRequired();
        builder.Property(m => m.AddedAt).IsRequired();

        builder.HasIndex(m => new { m.GroupId, m.UserId }).IsUnique();

        // Answers "which groups is this student in", behind My groups and My schedule.
        builder.HasIndex(m => m.UserId);
    }
}
