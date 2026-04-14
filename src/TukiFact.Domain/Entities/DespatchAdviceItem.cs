namespace TukiFact.Domain.Entities;

public class DespatchAdviceItem
{
    public Guid Id { get; set; }
    public Guid DespatchAdviceId { get; set; }

    public int LineNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public decimal Quantity { get; set; }
    public string UnitCode { get; set; } = "NIU"; // NIU=Unidad, KGM=Kilo, LTR=Litro

    // Navigation
    public DespatchAdvice DespatchAdvice { get; set; } = null!;
}
