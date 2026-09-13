using LF.AppDomain.Entities.News;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class NewsImageConfiguration : IEntityTypeConfiguration<NewsImage>
{
    public void Configure(EntityTypeBuilder<NewsImage> builder)
    {
        builder.ToTable("LFNewsImages");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();

        builder.Property(i => i.SortOrder).IsRequired();

        builder.HasOne(i => i.StorageObject)
            .WithMany()
            .HasForeignKey(i => i.StorageObjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
