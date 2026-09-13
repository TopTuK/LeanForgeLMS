using LF.AppDomain.Entities.News;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class NewsReadMarkerConfiguration : IEntityTypeConfiguration<NewsReadMarker>
{
    public void Configure(EntityTypeBuilder<NewsReadMarker> builder)
    {
        builder.ToTable("LFNewsReadMarkers");

        // UserId points across the ownership boundary into LF.IdentityService's Users table, so it
        // is a bare scalar key rather than an FK.
        builder.HasKey(m => m.UserId);
        builder.Property(m => m.UserId).ValueGeneratedNever();

        builder.Property(m => m.LastSeenAt).IsRequired();
    }
}
