using LF.AppDomain.Entities.Course;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.ToTable("LFPromoCodes");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.Code).IsRequired().HasMaxLength(PromoCode.MaxCodeLength);
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.DiscountType).IsRequired();
        builder.Property(p => p.DiscountValue).HasColumnType("numeric(12,2)");
        builder.Property(p => p.IsActive).IsRequired();

        // Users live in a separate bounded context (LF.IdentityService); this is a plain scalar
        // reference by convention, not an EF navigation/FK, even though it's physically the same DB.
        builder.Property(p => p.CreatedByUserId).IsRequired();

        builder.HasOne(p => p.Course)
            .WithMany()
            .HasForeignKey(p => p.CourseId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
