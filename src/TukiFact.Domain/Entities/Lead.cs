namespace TukiFact.Domain.Entities;

/// <summary>
/// Inbound lead from the public marketing site (tukifact.pe/contacto).
/// No TenantId — leads are pre-customer, owned by the platform sales team.
/// </summary>
public class Lead
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Phone { get; set; }
    public string Reason { get; set; } = "general"; // ventas | integracion | soporte | general
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = "website"; // website | landing | referral | manual
    public string Status { get; set; } = "new";     // new | contacted | qualified | dropped
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ContactedAt { get; set; }
    public string? Notes { get; set; }
}
