using HBP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HBP.Infrastructure.Persistence.Configurations;

public class RoomTypeAmenityConfiguration : IEntityTypeConfiguration<RoomTypeAmenity>
{
    public void Configure(EntityTypeBuilder<RoomTypeAmenity> builder)
    {
        builder.ToTable("room_type_amenities");

        builder.HasKey(e => new { e.RoomTypeId, e.AmenityId });

        builder.Property(e => e.RoomTypeId)
            .HasColumnName("room_type_id");

        builder.Property(e => e.AmenityId)
            .HasColumnName("amenity_id");

        // schema: display_order INTEGER DEFAULT 0 (nullable — no NOT NULL).
        builder.Property(e => e.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0);

        builder.HasOne(e => e.RoomType)
            .WithMany(r => r.RoomTypeAmenities)
            .HasForeignKey(e => e.RoomTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Amenity)
            .WithMany(a => a.RoomTypeAmenities)
            .HasForeignKey(e => e.AmenityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
