namespace HBP.Domain.Enums;

/// <summary>Maps to PostgreSQL enum type <c>email_status</c>.</summary>
public enum EmailStatus
{
    PENDING,
    SENT,
    RETRYING,
    FAILED
}
