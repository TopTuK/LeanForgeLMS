using LF.AppDomain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class StorageObjectConfiguration : IEntityTypeConfiguration<StorageObject>
{
    public void Configure(EntityTypeBuilder<StorageObject> builder)
    {
        builder.ToTable("LFStorageObjects");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.ObjectType).IsRequired();
        builder.Property(s => s.ObjectKey).IsRequired().HasMaxLength(260);
        builder.Property(s => s.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(s => s.SizeBytes).IsRequired();
        builder.Property(s => s.CreatedByUserId).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
    }
}
