namespace TukiFact.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    // Document
    public string DocType { get; set; } = "6";                 // 6=RUC, 1=DNI, 4=CE, 7=Pasaporte, 0=Sin doc
    public string DocNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;           // Razón social o nombre completo

    // Contact
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    // Location
    public string? Ubigeo { get; set; }
    public string? Departamento { get; set; }
    public string? Provincia { get; set; }
    public string? Distrito { get; set; }

    // Classification
    public string? Category { get; set; }                      // VIP, Regular, etc.
    public string? Notes { get; set; }

    // Status
    public bool IsActive { get; set; } = true;

    // Metadata
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
