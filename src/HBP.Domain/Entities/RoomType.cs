using HBP.Domain.Enums;

namespace HBP.Domain.Entities;

/// <summary>Maps to table <c>room_types</c>.</summary>
public class RoomType
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string NameVi { get; set; } = null!;
    public string? NameJa { get; set; }
    public string? ShortDescriptionVi { get; set; }
    public string? ShortDescriptionJa { get; set; }
    public string? DescriptionVi { get; set; }
    public string? DescriptionJa { get; set; }
    public decimal? PriceVnd { get; set; }
    public decimal? PriceUsd { get; set; }
    public PriceDisplayMode PriceDisplayMode { get; set; }
    public int Capacity { get; set; }
    public decimal? AreaSquareMeters { get; set; }
    public string? BedDescriptionVi { get; set; }
    public string? BedDescriptionJa { get; set; }
    public Guid? FeaturedMediaId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; }
    public string? SeoTitleVi { get; set; }
    public string? SeoTitleJa { get; set; }
    public string? SeoDescriptionVi { get; set; }
    public string? SeoDescriptionJa { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public MediaFile? FeaturedMedia { get; set; }
    public ICollection<RoomTypeAmenity> RoomTypeAmenities { get; set; } = new List<RoomTypeAmenity>();
    public ICollection<RoomTypeMedia> RoomTypeMedia { get; set; } = new List<RoomTypeMedia>();
}
