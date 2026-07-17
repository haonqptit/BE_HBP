using HBP.Application.Abstractions;
using HBP.Application.Requests;
using HBP.Domain.Entities;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HBP.Infrastructure.Requests;

public sealed class ContactRequestService(HbpDbContext db, IReferenceCodeGenerator generator,
    ILogger<ContactRequestService> logger) : IContactRequestService
{
    public async Task<SubmissionResponse> CreateAsync(CreateContactRequestRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Website))
        { logger.LogWarning("Contact honeypot triggered"); return new SubmissionResponse(generator.GenerateContactCode()); }
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var entity = new ContactRequest { ReferenceCode = generator.GenerateContactCode(), FullName = request.FullName.Trim(),
                Email = request.Email.Trim().ToLowerInvariant(), PhoneNumber = request.PhoneNumber.Trim(), Subject = request.Subject.Trim(),
                Message = request.Message.Trim(), LanguageCode = request.LanguageCode };
            try
            {
                db.ContactRequests.Add(entity); await db.SaveChangesAsync(cancellationToken);
                await RequestSubmissionHelper.AddDeliveriesAsync(db, "ContactRequest", entity.Id, "CONTACT_ADMIN_NOTIFICATION",
                    "CONTACT_GUEST_CONFIRMATION", entity.Email, entity.LanguageCode, logger, cancellationToken);
                await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
                return new SubmissionResponse(entity.ReferenceCode);
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "uq_contact_requests_reference_code" } && attempt < 3)
            { await transaction.RollbackAsync(cancellationToken); db.ChangeTracker.Clear(); }
        }
        throw new InvalidOperationException("Unable to generate a unique contact reference code after three attempts.");
    }
}
