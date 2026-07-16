using HBP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HBP.Infrastructure.Persistence.Configurations;

public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("admin_users");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Username)
            .HasColumnName("username")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(e => e.LastLoginAt)
            .HasColumnName("last_login_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.FailedCount)
            .HasColumnName("failed_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(e => e.FirstFailedAt)
            .HasColumnName("first_failed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.LockedUntil)
            .HasColumnName("locked_until")
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        // NOTE: schema.sql declares these as functional unique indexes on lower(username) /
        // lower(email). EF Core cannot model expression indexes; the raw SQL for
        // uq_admin_users_username_lower / uq_admin_users_email_lower will be applied in the
        // migration step. A plain unique index is declared here as the closest model equivalent.
        builder.HasIndex(e => e.Username).IsUnique().HasDatabaseName("uq_admin_users_username_lower");
        builder.HasIndex(e => e.Email).IsUnique().HasDatabaseName("uq_admin_users_email_lower");
    }
}
