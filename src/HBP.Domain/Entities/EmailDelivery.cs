using HBP.Domain.Enums;

namespace HBP.Domain.Entities;

/// <summary>
/// Maps to table <c>email_deliveries</c>. Polymorphic association: <see cref="RelatedEntityType"/>
/// (constrained to 'BookingRequest' or 'ContactRequest') + <see cref="RelatedEntityId"/> reference
/// the related entity without a database-level foreign key — resolved at the application layer.
/// </summary>
public class EmailDelivery
{
    public Guid Id { get; set; }
    public string RelatedEntityType { get; set; } = null!;
    public Guid RelatedEntityId { get; set; }
    public string EmailType { get; set; } = null!;
    public string Recipient { get; set; } = null!;
    public LanguageCode LanguageCode { get; set; }
    public EmailStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
}
