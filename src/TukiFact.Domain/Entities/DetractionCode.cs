namespace TukiFact.Domain.Entities;

public class DetractionCode
{
    public string Code { get; set; } = string.Empty;       // "037"
    public string Description { get; set; } = string.Empty; // "Demás servicios gravados con IGV"
    public decimal Percentage { get; set; }                  // 12.00
    public string Annex { get; set; } = string.Empty;        // "I", "II", "III"
    public bool IsActive { get; set; } = true;
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
}
