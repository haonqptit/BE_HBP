namespace HBP.Domain.Entities;

/// <summary>Maps to table <c>amenities</c>.</summary>
public class Amenity
{
    public Guid Id { get; set; }
    public string NameVi { get; set; } = null!;
    public string? NameJa { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ICollection<RoomTypeAmenity> RoomTypeAmenities { get; set; } = new List<RoomTypeAmenity>();
}
