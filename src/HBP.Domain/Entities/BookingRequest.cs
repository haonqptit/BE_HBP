using HBP.Domain.Enums;

namespace HBP.Domain.Entities;

/// <summary>Maps to table <c>booking_requests</c>.</summary>
public class BookingRequest
{
    public Guid Id { get; set; }
    public string ReferenceCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public Guid? RoomTypeId { get; set; }
    public DateOnly? CheckInDate { get; set; }
    public DateOnly? CheckOutDate { get; set; }
    public int Adults { get; set; }
    public int? Children { get; set; }
    public int? NumberOfRooms { get; set; }
    public string? CustomerMessage { get; set; }
    public LanguageCode LanguageCode { get; set; }
    public BookingRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public RoomType? RoomType { get; set; }
}
