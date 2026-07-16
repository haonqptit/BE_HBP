namespace HBP.Domain.Entities;

/// <summary>Maps to table <c>admin_users</c>.</summary>
public class AdminUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int FailedCount { get; set; }
    public DateTime? FirstFailedAt { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
