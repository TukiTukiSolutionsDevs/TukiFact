namespace TukiFact.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Ruc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string? Direccion { get; set; }
    public string? Ubigeo { get; set; }
    public string? Departamento { get; set; }
    public string? Provincia { get; set; }
    public string? Distrito { get; set; }
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#1a73e8";
    public Guid? PlanId { get; set; }
    public byte[]? CertificateData { get; set; }
    public string? CertificatePasswordEncrypted { get; set; }
    public DateTimeOffset? CertificateExpiresAt { get; set; }
    public string? SunatUser { get; set; }
    public string? SunatPasswordEncrypted { get; set; }
    public string Environment { get; set; } = "beta";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Plan? Plan { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    public ICollection<Series> Series { get; set; } = new List<Series>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public TenantServiceConfig? ServiceConfig { get; set; }
}
