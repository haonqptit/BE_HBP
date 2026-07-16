namespace HBP.Domain.Entities;

/// <summary>Maps to table <c>services</c>.</summary>
public class Service
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = null!;
    public string NameVi { get; set; } = null!;
    public string? NameJa { get; set; }
    public string? ShortDescriptionVi { get; set; }
    public string? ShortDescriptionJa { get; set; }
    public string? DescriptionVi { get; set; }
    public string? DescriptionJa { get; set; }
    public string? PriceNoteVi { get; set; }
    public string? PriceNoteJa { get; set; }
    public Guid? FeaturedMediaId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public MediaFile? FeaturedMedia { get; set; }
}
