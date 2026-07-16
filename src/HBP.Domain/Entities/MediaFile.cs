namespace HBP.Domain.Entities;

/// <summary>Maps to table <c>media_files</c>.</summary>
public class MediaFile
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = null!;
    public string StoredFileName { get; set; } = null!;
    public string StoragePath { get; set; } = null!;
    public string PublicUrl { get; set; } = null!;
    public string MimeType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? AltTextVi { get; set; }
    public string? AltTextJa { get; set; }
    public DateTime CreatedAt { get; set; }
}
