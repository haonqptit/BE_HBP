using HBP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HBP.Infrastructure.Persistence.Configurations;

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");

        builder.HasKey(e => e.Key);

        builder.Property(e => e.Key)
            .HasColumnName("key")
            .HasMaxLength(100);

        builder.Property(e => e.Value)
            .HasColumnName("value")
            .HasColumnType("text");

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(255);

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();
    }
}
