using LF.AppDomain.Entities.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class CoursePaymentConfiguration : IEntityTypeConfiguration<CoursePayment>
{
    public void Configure(EntityTypeBuilder<CoursePayment> builder)
    {
        builder.ToTable("LFCoursePayments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        // PaymentOrderId / EnrollmentId / UserId / CourseId are plain scalar snapshots, not EF
        // navigations/FKs — this ledger deliberately outlives the rows it was projected from.
        builder.Property(p => p.PaymentOrderId).IsRequired();
        builder.Property(p => p.EnrollmentId).IsRequired();
        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.CourseId).IsRequired();

        builder.Property(p => p.StudentEmail).IsRequired().HasMaxLength(CoursePayment.MaxEmailLength);
        builder.Property(p => p.StudentName).IsRequired().HasMaxLength(CoursePayment.MaxNameLength);
        builder.Property(p => p.CourseTitle).IsRequired().HasMaxLength(CoursePayment.MaxCourseTitleLength);
        builder.Property(p => p.Amount).HasColumnType("numeric(12,2)");
        builder.Property(p => p.PromoCode).HasMaxLength(CoursePayment.MaxPromoCodeLength);
        builder.Property(p => p.Provider).IsRequired().HasMaxLength(CoursePayment.MaxProviderLength);
        builder.Property(p => p.ProviderOperationId).HasMaxLength(CoursePayment.MaxProviderOperationIdLength);
        builder.Property(p => p.PaidAt).IsRequired();
        builder.Property(p => p.RecordedAt).IsRequired();

        builder.HasIndex(p => p.PaymentOrderId).IsUnique();
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.CourseId);
        builder.HasIndex(p => p.EnrollmentId);
        builder.HasIndex(p => p.PaidAt);
    }
}
