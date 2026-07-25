using HBP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HBP.Infrastructure.Persistence.Configurations;

public class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
{
    public void Configure(EntityTypeBuilder<RoomType> builder)
    {
        builder.ToTable("room_types", t =>
        {
            t.HasCheckConstraint("ck_room_types_price_vnd", "price_vnd IS NULL OR price_vnd >= 0");
            t.HasCheckConstraint("ck_room_types_price_usd", "price_usd IS NULL OR price_usd >= 0");
            t.HasCheckConstraint("ck_room_types_capacity", "capacity >= 1");
            t.HasCheckConstraint("ck_room_types_display_order", "display_order >= 0");
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

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

        builder.Property(e => e.PriceVnd)
            .HasColumnName("price_vnd")
            .HasColumnType("numeric(14,2)");

        builder.Property(e => e.PriceUsd)
            .HasColumnName("price_usd")
            .HasColumnType("numeric(10,2)");

        builder.Property(e => e.PriceDisplayMode)
            .HasColumnName("price_display_mode")
            .HasDefaultValue(Domain.Enums.PriceDisplayMode.CONTACT)
            .HasSentinel((Domain.Enums.PriceDisplayMode)(-1))
            .IsRequired();

        builder.Property(e => e.Capacity)
            .HasColumnName("capacity")
            .IsRequired();

        builder.Property(e => e.AreaSquareMeters)
            .HasColumnName("area_square_meters")
            .HasColumnType("numeric(6,2)");

        builder.Property(e => e.BedDescriptionVi)
            .HasColumnName("bed_description_vi")
            .HasMaxLength(255);

        builder.Property(e => e.BedDescriptionJa)
            .HasColumnName("bed_description_ja")
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

        builder.Property(e => e.SeoTitleVi)
            .HasColumnName("seo_title_vi")
            .HasMaxLength(255);

        builder.Property(e => e.SeoTitleJa)
            .HasColumnName("seo_title_ja")
            .HasMaxLength(255);

        builder.Property(e => e.SeoDescriptionVi)
            .HasColumnName("seo_description_vi")
            .HasMaxLength(500);

        builder.Property(e => e.SeoDescriptionJa)
            .HasColumnName("seo_description_ja")
            .HasMaxLength(500);

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

        builder.HasIndex(e => e.Code).IsUnique().HasDatabaseName("uq_room_types_code");
        builder.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("uq_room_types_slug");
        builder.HasIndex(e => new { e.IsVisible, e.DisplayOrder })
            .HasDatabaseName("idx_room_types_visible_order");

        builder.HasOne(e => e.FeaturedMedia)
            .WithMany()
            .HasForeignKey(e => e.FeaturedMediaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
