using HBP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HBP.Infrastructure.Persistence.Configurations;

public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.ToTable("amenities", t =>
            t.HasCheckConstraint("ck_amenities_display_order", "display_order >= 0"));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.NameVi)
            .HasColumnName("name_vi")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.NameJa)
            .HasColumnName("name_ja")
            .HasMaxLength(150);

        builder.Property(e => e.Icon)
            .HasColumnName("icon")
            .HasMaxLength(100);

        builder.Property(e => e.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(e => e.IsVisible)
            .HasColumnName("is_visible")
            .HasDefaultValue(true)
            .IsRequired();

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
    }
}
