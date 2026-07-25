using HBP.Application.Common;
using HBP.Domain.Enums;

namespace HBP.Application.Admin;

public sealed record AdminEmailDeliveryResponse(Guid Id, string EmailType, string Recipient,
    LanguageCode LanguageCode, EmailStatus Status, int AttemptCount, DateTime? NextRetryAt,
    DateTime? LastAttemptAt, DateTime? SentAt, string? LastError, DateTime CreatedAt);

public sealed record AdminBookingRequestListItem(Guid Id, string ReferenceCode, string FullName, string Email,
    string PhoneNumber, string? RoomTypeName, DateOnly? CheckInDate, DateOnly? CheckOutDate, int Adults,
    int? Children, int? NumberOfRooms, LanguageCode LanguageCode, BookingRequestStatus Status, DateTime CreatedAt);

public sealed record AdminBookingRequestResponse(Guid Id, string ReferenceCode, string FullName, string Email,
    string PhoneNumber, Guid? RoomTypeId, string? RoomTypeName, string? RoomTypeSlug, DateOnly? CheckInDate,
    DateOnly? CheckOutDate, int Adults, int? Children, int? NumberOfRooms, string? CustomerMessage,
    LanguageCode LanguageCode, BookingRequestStatus Status, DateTime CreatedAt,
    IReadOnlyList<AdminEmailDeliveryResponse> EmailDeliveries);

public sealed record AdminContactRequestListItem(Guid Id, string ReferenceCode, string FullName, string Email,
    string PhoneNumber, string Subject, LanguageCode LanguageCode, DateTime CreatedAt);

public sealed record AdminContactRequestResponse(Guid Id, string ReferenceCode, string FullName, string Email,
    string PhoneNumber, string Subject, string Message, LanguageCode LanguageCode, DateTime CreatedAt,
    IReadOnlyList<AdminEmailDeliveryResponse> EmailDeliveries);

public interface IAdminBookingRequestService
{
    Task<PagedResult<AdminBookingRequestListItem>> ListAsync(PageQuery query, CancellationToken cancellationToken);
    Task<AdminBookingRequestResponse> GetAsync(Guid id, CancellationToken cancellationToken);
}

public interface IAdminContactRequestService
{
    Task<PagedResult<AdminContactRequestListItem>> ListAsync(PageQuery query, CancellationToken cancellationToken);
    Task<AdminContactRequestResponse> GetAsync(Guid id, CancellationToken cancellationToken);
}
