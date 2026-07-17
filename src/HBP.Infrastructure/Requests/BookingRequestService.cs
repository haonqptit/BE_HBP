using HBP.Application.Abstractions;
using HBP.Application.Common;
using HBP.Application.Requests;
using HBP.Domain.Entities;
using HBP.Domain.Enums;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HBP.Infrastructure.Requests;

public sealed class BookingRequestService(HbpDbContext db, IReferenceCodeGenerator generator,
    ILogger<BookingRequestService> logger) : IBookingRequestService
{
    public async Task<SubmissionResponse> CreateAsync(CreateBookingRequestRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Website))
        { logger.LogWarning("Booking honeypot triggered"); return new SubmissionResponse(generator.GenerateBookingCode()); }
        if (request.RoomTypeId.HasValue && !await db.RoomTypes.AsNoTracking()
                .AnyAsync(x => x.Id == request.RoomTypeId && x.IsVisible, cancellationToken))
            throw new ValidationException("Invalid room type.", new Dictionary<string, string[]> { ["roomTypeId"] = ["Room type must exist and be visible."] });

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var entity = new BookingRequest { ReferenceCode = generator.GenerateBookingCode(), FullName = request.FullName.Trim(),
                Email = request.Email.Trim().ToLowerInvariant(), PhoneNumber = request.PhoneNumber.Trim(), RoomTypeId = request.RoomTypeId,
                CheckInDate = request.CheckInDate, CheckOutDate = request.CheckOutDate, Adults = request.Adults, Children = request.Children,
                NumberOfRooms = request.NumberOfRooms, CustomerMessage = request.CustomerMessage?.Trim(), LanguageCode = request.LanguageCode,
                Status = BookingRequestStatus.RECEIVED };
            try
            {
                db.BookingRequests.Add(entity); await db.SaveChangesAsync(cancellationToken);
                await RequestSubmissionHelper.AddDeliveriesAsync(db, "BookingRequest", entity.Id, "BOOKING_ADMIN_NOTIFICATION",
                    "BOOKING_GUEST_CONFIRMATION", entity.Email, entity.LanguageCode, logger, cancellationToken);
                await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
                return new SubmissionResponse(entity.ReferenceCode);
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "uq_booking_requests_reference_code" } && attempt < 3)
            { await transaction.RollbackAsync(cancellationToken); db.ChangeTracker.Clear(); }
        }
        throw new InvalidOperationException("Unable to generate a unique booking reference code after three attempts.");
    }
}
