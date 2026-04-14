namespace TukiFact.Domain.Entities;

public class DespatchAdvice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    // Document identification
    public string DocumentType { get; set; } = "09"; // 09=GRE Remitente, 31=GRE Transportista
    public string Serie { get; set; } = string.Empty; // T001 (remitente), V001 (transportista)
    public long Correlative { get; set; }
    public string FullNumber => $"{Serie}-{Correlative:D8}";

    // Dates
    public DateOnly IssueDate { get; set; }
    public TimeOnly IssueTime { get; set; }
    public DateOnly TransferStartDate { get; set; }

    // Transfer reason (Catálogo 20)
    public string TransferReasonCode { get; set; } = "01"; // 01=Venta, 04=Traslado entre establecimientos, etc.
    public string TransferReasonDescription { get; set; } = "Venta";
    public string? Note { get; set; }

    // Weight and packages
    public decimal GrossWeight { get; set; }
    public string WeightUnitCode { get; set; } = "KGM";
    public int TotalPackages { get; set; }

    // Transport mode: 01=Público, 02=Privado
    public string TransportMode { get; set; } = "01";

    // Carrier (transporte público)
    public string? CarrierDocType { get; set; } // 6=RUC
    public string? CarrierDocNumber { get; set; }
    public string? CarrierName { get; set; }
    public string? CarrierMtcNumber { get; set; } // Número MTC

    // Driver (transporte privado)
    public string? DriverDocType { get; set; } // 1=DNI
    public string? DriverDocNumber { get; set; }
    public string? DriverName { get; set; }
    public string? DriverLicense { get; set; }

    // Vehicle
    public string? VehiclePlate { get; set; }
    public string? SecondaryVehiclePlate { get; set; }

    // Recipient (destinatario)
    public string RecipientDocType { get; set; } = "6";
    public string RecipientDocNumber { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;

    // Origin address
    public string OriginUbigeo { get; set; } = string.Empty;
    public string OriginAddress { get; set; } = string.Empty;

    // Destination address
    public string DestinationUbigeo { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;

    // Related document (factura, boleta, etc.)
    public string? RelatedDocType { get; set; }
    public string? RelatedDocNumber { get; set; }

    // SUNAT status
    public string Status { get; set; } = "draft"; // draft, signed, sent, accepted, rejected
    public string? SunatResponseCode { get; set; }
    public string? SunatResponseMessage { get; set; }
    public string? SunatTicket { get; set; }

    // Files (MinIO paths)
    public string? XmlUrl { get; set; }
    public string? PdfUrl { get; set; }
    public string? CdrUrl { get; set; }

    // Audit
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid CreatedByUserId { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<DespatchAdviceItem> Items { get; set; } = new List<DespatchAdviceItem>();
}
