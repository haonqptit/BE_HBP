using HBP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HBP.Infrastructure.Persistence.Configurations;

public class RoomTypeMediaConfiguration : IEntityTypeConfiguration<RoomTypeMedia>
{
    public void Configure(EntityTypeBuilder<RoomTypeMedia> builder)
    {
        builder.ToTable("room_type_media", t =>
            t.HasCheckConstraint("ck_room_type_media_display_order", "display_order >= 0"));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.RoomTypeId)
            .HasColumnName("room_type_id");

        builder.Property(e => e.MediaFileId)
            .HasColumnName("media_file_id");

        builder.Property(e => e.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(e => new { e.RoomTypeId, e.MediaFileId })
            .IsUnique()
            .HasDatabaseName("uq_room_type_media");

        builder.HasOne(e => e.RoomType)
            .WithMany(r => r.RoomTypeMedia)
            .HasForeignKey(e => e.RoomTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.MediaFile)
            .WithMany()
            .HasForeignKey(e => e.MediaFileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
