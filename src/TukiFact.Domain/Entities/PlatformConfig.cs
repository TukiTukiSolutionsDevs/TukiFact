namespace TukiFact.Domain.Entities;

/// <summary>
/// Global platform configuration (key-value store).
/// Managed by superadmin via backoffice.
/// </summary>
public class PlatformConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
