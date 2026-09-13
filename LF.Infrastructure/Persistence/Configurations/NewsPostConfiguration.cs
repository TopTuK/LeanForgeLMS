using LF.AppDomain.Entities.News;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class NewsPostConfiguration : IEntityTypeConfiguration<NewsPost>
{
    public void Configure(EntityTypeBuilder<NewsPost> builder)
    {
        builder.ToTable("LFNewsPosts");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.Title).IsRequired().HasMaxLength(NewsPost.MaxTitleLength);
        builder.Property(p => p.Html).IsRequired();
        builder.Property(p => p.Visibility).IsRequired();
        builder.Property(p => p.IsPublished).IsRequired();
        builder.Property(p => p.PublishedAt);
        builder.Property(p => p.CreatedByUserId).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(i => i.NewsPostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Images).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => new { p.IsPublished, p.Visibility, p.PublishedAt });
    }
}
