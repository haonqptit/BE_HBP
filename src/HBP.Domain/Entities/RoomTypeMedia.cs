namespace HBP.Domain.Entities;

/// <summary>Junction table <c>room_type_media</c> (detail images).</summary>
public class RoomTypeMedia
{
    public Guid Id { get; set; }
    public Guid RoomTypeId { get; set; }
    public Guid MediaFileId { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public RoomType RoomType { get; set; } = null!;
    public MediaFile MediaFile { get; set; } = null!;
}
