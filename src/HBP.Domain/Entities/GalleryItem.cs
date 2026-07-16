namespace HBP.Domain.Entities;

/// <summary>Maps to table <c>gallery_items</c>.</summary>
public class GalleryItem
{
    public Guid Id { get; set; }
    public Guid MediaFileId { get; set; }
    public Guid GalleryCategoryId { get; set; }
    public string? CaptionVi { get; set; }
    public string? CaptionJa { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public MediaFile MediaFile { get; set; } = null!;
    public GalleryCategory GalleryCategory { get; set; } = null!;
}
