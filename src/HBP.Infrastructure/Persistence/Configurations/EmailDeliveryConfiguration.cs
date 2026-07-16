using HBP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HBP.Infrastructure.Persistence.Configurations;

public class EmailDeliveryConfiguration : IEntityTypeConfiguration<EmailDelivery>
{
    public void Configure(EntityTypeBuilder<EmailDelivery> builder)
    {
        builder.ToTable("email_deliveries", t =>
        {
            t.HasCheckConstraint("ck_email_deliveries_related_entity_type",
                "related_entity_type IN ('BookingRequest', 'ContactRequest')");
            t.HasCheckConstraint("ck_email_deliveries_attempt_count", "attempt_count >= 0");
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        // Polymorphic association — no database-level FK; resolved at the application layer.
        builder.Property(e => e.RelatedEntityType)
            .HasColumnName("related_entity_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.RelatedEntityId)
            .HasColumnName("related_entity_id")
            .IsRequired();

        builder.Property(e => e.EmailType)
            .HasColumnName("email_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Recipient)
            .HasColumnName("recipient")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.LanguageCode)
            .HasColumnName("language_code")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasDefaultValue(Domain.Enums.EmailStatus.PENDING)
            .IsRequired();

        builder.Property(e => e.AttemptCount)
            .HasColumnName("attempt_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(e => e.NextRetryAt)
            .HasColumnName("next_retry_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.LastAttemptAt)
            .HasColumnName("last_attempt_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.SentAt)
            .HasColumnName("sent_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(1000);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("idx_email_deliveries_status");

        builder.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityId })
            .HasDatabaseName("idx_email_deliveries_related_entity");

        // Partial index: WHERE status = 'RETRYING'.
        builder.HasIndex(e => e.NextRetryAt)
            .HasDatabaseName("idx_email_deliveries_next_retry")
            .HasFilter("status = 'RETRYING'");
    }
}
