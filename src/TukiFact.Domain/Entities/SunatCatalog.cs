namespace TukiFact.Domain.Entities;

public class SunatCatalog
{
    public string CatalogNumber { get; set; } = string.Empty; // "01", "06", "07", "54"
    public string Name { get; set; } = string.Empty;           // "Tipo de Documento"
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<SunatCatalogCode> Codes { get; set; } = new List<SunatCatalogCode>();
}
