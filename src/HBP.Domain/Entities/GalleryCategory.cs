namespace HBP.Domain.Entities;

/// <summary>Maps to table <c>gallery_categories</c>.</summary>
public class GalleryCategory
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = null!;
    public string NameVi { get; set; } = null!;
    public string? NameJa { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ICollection<GalleryItem> GalleryItems { get; set; } = new List<GalleryItem>();
}
