using LF.AppDomain.Entities.Course;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class LessonPartFileConfiguration : IEntityTypeConfiguration<LessonPartFile>
{
    public void Configure(EntityTypeBuilder<LessonPartFile> builder)
    {
        builder.ToTable("LFLessonPartFiles");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedOnAdd();

        builder.Property(f => f.FileName).IsRequired().HasMaxLength(260);
        builder.Property(f => f.SortOrder).IsRequired();

        builder.HasOne(f => f.StorageObject)
            .WithMany()
            .HasForeignKey(f => f.StorageObjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
