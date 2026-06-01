namespace TukiFact.Application.DTOs.DespatchAdvices;

public record DespatchAdviceResponse(
    Guid Id,
    string DocumentType,
    string DocumentTypeName,
    string Serie,
    long Correlative,
    string FullNumber,
    DateOnly IssueDate,
    TimeOnly IssueTime,
    DateOnly TransferStartDate,
    string TransferReasonCode,
    string TransferReasonDescription,
    string? Note,
    decimal GrossWeight,
    string WeightUnitCode,
    int TotalPackages,
    string TransportMode,
    string TransportModeName,
    // Carrier
    string? CarrierDocType,
    string? CarrierDocNumber,
    string? CarrierName,
    // Driver
    string? DriverDocType,
    string? DriverDocNumber,
    string? DriverName,
    string? DriverLicense,
    // Vehicle
    string? VehiclePlate,
    // Recipient
    string RecipientDocType,
    string RecipientDocNumber,
    string RecipientName,
    // Addresses
    string OriginUbigeo,
    string OriginAddress,
    string DestinationUbigeo,
    string DestinationAddress,
    // Related document
    string? RelatedDocType,
    string? RelatedDocNumber,
    // Status
    string Status,
    string? SunatResponseCode,
    string? SunatResponseMessage,
    string? SunatTicket,
    string? XmlUrl,
    string? PdfUrl,
    string? CdrUrl,
    DateTimeOffset CreatedAt,
    List<DespatchAdviceItemResponse> Items
);

public record DespatchAdviceItemResponse(
    int LineNumber,
    string Description,
    string? ProductCode,
    decimal Quantity,
    string UnitCode
);
