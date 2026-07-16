using HBP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HBP.Infrastructure.Persistence.Configurations;

public class ContactRequestConfiguration : IEntityTypeConfiguration<ContactRequest>
{
    public void Configure(EntityTypeBuilder<ContactRequest> builder)
    {
        builder.ToTable("contact_requests");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.ReferenceCode)
            .HasColumnName("reference_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Subject)
            .HasColumnName("subject")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Message)
            .HasColumnName("message")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.LanguageCode)
            .HasColumnName("language_code")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(e => e.ReferenceCode)
            .IsUnique()
            .HasDatabaseName("uq_contact_requests_reference_code");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("idx_contact_requests_created_at")
            .IsDescending();

        // NOTE: schema.sql also declares GIN pg_trgm indexes on full_name / email
        // (idx_contact_requests_*_trgm). GIN trigram indexes cannot be expressed in the EF model
        // and will be added as raw SQL in the migration step.
    }
}
