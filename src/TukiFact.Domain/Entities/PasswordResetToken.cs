namespace TukiFact.Domain.Entities;

/// <summary>
/// Token for password reset flow.
/// Generated when user requests "Forgot Password", validated on reset.
/// </summary>
public class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty; // Random secure token
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UsedAt { get; set; } // null = not used yet
    public bool IsUsed => UsedAt.HasValue;
    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;

    // Navigation
    public User User { get; set; } = null!;
}
