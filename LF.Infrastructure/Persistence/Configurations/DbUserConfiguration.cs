using LF.AppDomain.Entities.User;
using LF.AppDomain.Models.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class DbUserConfiguration : IEntityTypeConfiguration<DbUser>
{
    public void Configure(EntityTypeBuilder<DbUser> builder)
    {
        builder.ToTable("LFUsers");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();

        builder.Property(u => u.Email).IsRequired();
        builder.Property(u => u.AvatarKey).HasMaxLength(260);
        builder.Property(u => u.Description).HasMaxLength(500);
        builder.Property(u => u.PreferredLanguage).HasMaxLength(UserLanguage.MaxLength);
    }
}
