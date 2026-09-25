using LF.AppDomain.Entities.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LF.Infrastructure.Persistence.Configurations;

internal sealed class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.ToTable("LFEmailMessages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();

        builder.Property(m => m.ToAddress).IsRequired().HasMaxLength(EmailMessage.MaxAddressLength);
        builder.Property(m => m.ToName).HasMaxLength(EmailMessage.MaxNameLength);
        builder.Property(m => m.Subject).IsRequired().HasMaxLength(EmailMessage.MaxSubjectLength);
        builder.Property(m => m.HtmlBody).IsRequired();
        builder.Property(m => m.Status).IsRequired();
        builder.Property(m => m.LastError).HasMaxLength(EmailMessage.MaxErrorLength);

        // Serves the dispatcher's "Pending and due, oldest first" poll.
        builder.HasIndex(m => new { m.Status, m.NextAttemptAt });
    }
}
