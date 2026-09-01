using LF.AppDomain.Entities.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class PaymentOrderConfiguration : IEntityTypeConfiguration<PaymentOrder>
{
    public void Configure(EntityTypeBuilder<PaymentOrder> builder)
    {
        builder.ToTable("LFPaymentOrders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd();

        // EnrollmentId / UserId are plain scalar references, not EF navigations/FKs — enrollments are
        // owned by LF.CourseService and users by LF.IdentityService, even though it's one physical DB.
        builder.Property(o => o.EnrollmentId).IsRequired();
        builder.Property(o => o.UserId).IsRequired();

        builder.Property(o => o.Amount).HasColumnType("numeric(12,2)");
        builder.Property(o => o.Description).IsRequired().HasMaxLength(PaymentOrder.MaxDescriptionLength);
        builder.Property(o => o.Provider).IsRequired().HasMaxLength(32);
        builder.Property(o => o.Status).IsRequired();
        builder.Property(o => o.ProviderOperationId).HasMaxLength(128);

        builder.HasIndex(o => o.EnrollmentId);
        builder.HasIndex(o => o.Status);
    }
}
