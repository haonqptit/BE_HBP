using HBP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HBP.Infrastructure.Persistence.Configurations;

public class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("media_files", t =>
            t.HasCheckConstraint("ck_media_files_size_bytes", "size_bytes >= 0"));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.StoredFileName)
            .HasColumnName("stored_file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.StoragePath)
            .HasColumnName("storage_path")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.PublicUrl)
            .HasColumnName("public_url")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.MimeType)
            .HasColumnName("mime_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.SizeBytes)
            .HasColumnName("size_bytes")
            .IsRequired();

        builder.Property(e => e.Width)
            .HasColumnName("width");

        builder.Property(e => e.Height)
            .HasColumnName("height");

        builder.Property(e => e.AltTextVi)
            .HasColumnName("alt_text_vi")
            .HasMaxLength(255);

        builder.Property(e => e.AltTextJa)
            .HasColumnName("alt_text_ja")
            .HasMaxLength(255);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();
    }
}
