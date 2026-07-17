using HBP.Domain.Enums;

namespace HBP.Application.Requests;

public sealed record CreateBookingRequestRequest(string FullName, string Email, string PhoneNumber,
    Guid? RoomTypeId, DateOnly? CheckInDate, DateOnly? CheckOutDate, int Adults, int? Children,
    int? NumberOfRooms, string? CustomerMessage, LanguageCode LanguageCode, string? Website);
public sealed record CreateContactRequestRequest(string FullName, string Email, string PhoneNumber,
    string Subject, string Message, LanguageCode LanguageCode, string? Website);
public sealed record SubmissionResponse(string ReferenceCode);
public interface IBookingRequestService
{
    Task<SubmissionResponse> CreateAsync(CreateBookingRequestRequest request, CancellationToken cancellationToken);
}
public interface IContactRequestService
{
    Task<SubmissionResponse> CreateAsync(CreateContactRequestRequest request, CancellationToken cancellationToken);
}
