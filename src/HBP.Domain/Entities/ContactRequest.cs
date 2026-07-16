using HBP.Domain.Enums;

namespace HBP.Domain.Entities;

/// <summary>Maps to table <c>contact_requests</c>.</summary>
public class ContactRequest
{
    public Guid Id { get; set; }
    public string ReferenceCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Message { get; set; } = null!;
    public LanguageCode LanguageCode { get; set; }
    public DateTime CreatedAt { get; set; }
}
