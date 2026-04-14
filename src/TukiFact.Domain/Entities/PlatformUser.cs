namespace TukiFact.Domain.Entities;

/// <summary>
/// Internal platform user (superadmin, support, ops).
/// Separate from tenant User — NOT subject to RLS.
/// </summary>
public class PlatformUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "support"; // superadmin, support, ops
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
