using HBP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HBP.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("services", t =>
            t.HasCheckConstraint("ck_services_display_order", "display_order >= 0"));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Slug)
            .HasColumnName("slug")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.NameVi)
            .HasColumnName("name_vi")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.NameJa)
            .HasColumnName("name_ja")
            .HasMaxLength(255);

        builder.Property(e => e.ShortDescriptionVi)
            .HasColumnName("short_description_vi")
            .HasMaxLength(500);

        builder.Property(e => e.ShortDescriptionJa)
            .HasColumnName("short_description_ja")
            .HasMaxLength(500);

        builder.Property(e => e.DescriptionVi)
            .HasColumnName("description_vi")
            .HasColumnType("text");

        builder.Property(e => e.DescriptionJa)
            .HasColumnName("description_ja")
            .HasColumnType("text");

        builder.Property(e => e.PriceNoteVi)
            .HasColumnName("price_note_vi")
            .HasMaxLength(255);

        builder.Property(e => e.PriceNoteJa)
            .HasColumnName("price_note_ja")
            .HasMaxLength(255);

        builder.Property(e => e.FeaturedMediaId)
            .HasColumnName("featured_media_id");

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

        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("uq_services_slug");
        builder.HasIndex(e => new { e.IsVisible, e.DisplayOrder })
            .HasDatabaseName("idx_services_visible_order");

        builder.HasOne(e => e.FeaturedMedia)
            .WithMany()
            .HasForeignKey(e => e.FeaturedMediaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
