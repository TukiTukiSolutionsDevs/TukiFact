namespace TukiFact.Application.DTOs.DespatchAdvices;

public record CreateDespatchAdviceRequest(
    string DocumentType, // "09"=Remitente, "31"=Transportista
    string Serie, // T001, V001
    DateOnly? IssueDate,
    TimeOnly? IssueTime,
    DateOnly TransferStartDate,

    // Transfer reason (Catálogo 20)
    string TransferReasonCode, // 01=Venta, 04=Traslado, etc.
    string TransferReasonDescription,
    string? Note,

    // Weight
    decimal GrossWeight,
    string WeightUnitCode, // KGM, TNE
    int TotalPackages,

    // Transport mode
    string TransportMode, // 01=Público, 02=Privado

    // Carrier (transporte público)
    string? CarrierDocType,
    string? CarrierDocNumber,
    string? CarrierName,
    string? CarrierMtcNumber,

    // Driver (transporte privado)
    string? DriverDocType,
    string? DriverDocNumber,
    string? DriverName,
    string? DriverLicense,

    // Vehicle
    string? VehiclePlate,
    string? SecondaryVehiclePlate,

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

    // Items
    List<CreateDespatchAdviceItemRequest> Items
);

public record CreateDespatchAdviceItemRequest(
    string Description,
    string? ProductCode,
    decimal Quantity,
    string UnitCode // NIU, KGM, LTR
);
