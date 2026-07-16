using HBP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HBP.Infrastructure.Persistence.Configurations;

public class GalleryItemConfiguration : IEntityTypeConfiguration<GalleryItem>
{
    public void Configure(EntityTypeBuilder<GalleryItem> builder)
    {
        builder.ToTable("gallery_items", t =>
            t.HasCheckConstraint("ck_gallery_items_display_order", "display_order >= 0"));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.MediaFileId)
            .HasColumnName("media_file_id");

        builder.Property(e => e.GalleryCategoryId)
            .HasColumnName("gallery_category_id");

        builder.Property(e => e.CaptionVi)
            .HasColumnName("caption_vi")
            .HasMaxLength(255);

        builder.Property(e => e.CaptionJa)
            .HasColumnName("caption_ja")
            .HasMaxLength(255);

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

        builder.HasIndex(e => new { e.GalleryCategoryId, e.IsVisible, e.DisplayOrder })
            .HasDatabaseName("idx_gallery_items_category_visible_order");

        builder.HasOne(e => e.MediaFile)
            .WithMany()
            .HasForeignKey(e => e.MediaFileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.GalleryCategory)
            .WithMany(c => c.GalleryItems)
            .HasForeignKey(e => e.GalleryCategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
