using LF.AppDomain.Entities.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class PlatformSettingsConfiguration : IEntityTypeConfiguration<PlatformSettings>
{
    public void Configure(EntityTypeBuilder<PlatformSettings> builder)
    {
        builder.ToTable("LFPlatformSettings");

        builder.HasKey(s => s.Id);
        // Single fixed row (Id = PlatformSettings.SingletonId) — the app assigns the key, not the DB.
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.StudentEnrollmentEnabled).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
    }
}
