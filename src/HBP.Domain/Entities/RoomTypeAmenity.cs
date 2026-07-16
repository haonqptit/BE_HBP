namespace HBP.Domain.Entities;

/// <summary>Junction table <c>room_type_amenities</c> (composite PK).</summary>
public class RoomTypeAmenity
{
    public Guid RoomTypeId { get; set; }
    public Guid AmenityId { get; set; }
    public int? DisplayOrder { get; set; }

    // Navigation
    public RoomType RoomType { get; set; } = null!;
    public Amenity Amenity { get; set; } = null!;
}
