namespace TukiFact.Domain.Entities;

public class SunatCatalogCode
{
    public Guid Id { get; set; }
    public string CatalogNumber { get; set; } = string.Empty; // FK to SunatCatalog
    public string Code { get; set; } = string.Empty;           // "01", "6", "10"
    public string Description { get; set; } = string.Empty;    // "Factura", "RUC", "Gravado"
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    // Navigation
    public SunatCatalog Catalog { get; set; } = null!;
}
