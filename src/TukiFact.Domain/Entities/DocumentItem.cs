namespace TukiFact.Domain.Entities;

public class DocumentItem
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int Sequence { get; set; }

    // Product
    public string? ProductCode { get; set; }       // Código interno del producto
    public string? SunatProductCode { get; set; }   // Código SUNAT (tabla 25)
    public string Description { get; set; } = string.Empty;

    // Quantities
    public decimal Quantity { get; set; }
    public string UnitMeasure { get; set; } = "NIU"; // NIU=unidad, ZZ=servicio, KGM=kg
    public decimal UnitPrice { get; set; }            // Precio unitario sin IGV
    public decimal UnitPriceWithIgv { get; set; }     // Precio unitario con IGV

    // Tax
    public string IgvType { get; set; } = "10";  // 10=Gravado, 20=Exonerado, 30=Inafecto, 21=Gratuito
    public decimal IgvAmount { get; set; }

    // Amounts
    public decimal Subtotal { get; set; }     // Quantity * UnitPrice
    public decimal Discount { get; set; }
    public decimal Total { get; set; }        // Subtotal + IgvAmount - Discount

    // ICBPER (Impuesto Bolsas Plásticas — Ley 30884)
    public int IcbperBagQuantity { get; set; }                    // Cantidad de bolsas
    public decimal IcbperUnitAmount { get; set; } = 0.50m;        // S/ 0.50 por bolsa (2023+)
    public decimal IcbperTotal => IcbperBagQuantity * IcbperUnitAmount; // Total ICBPER del item

    // Navigation
    public Document Document { get; set; } = null!;
}
