using Microsoft.Extensions.Logging;
using TukiFact.Application.DTOs.DespatchAdvices;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Services;

public class DespatchAdviceService : IDespatchAdviceService
{
    private readonly IDespatchAdviceRepository _repository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IGreXmlBuilder _xmlBuilder;
    private readonly IGreSunatClient _greSunatClient;
    private readonly IXmlSigningService _xmlSigner;
    private readonly IStorageService _storageService;
    private readonly ILogger<DespatchAdviceService> _logger;

    public DespatchAdviceService(
        IDespatchAdviceRepository repository,
        ITenantRepository tenantRepository,
        IGreXmlBuilder xmlBuilder,
        IGreSunatClient greSunatClient,
        IXmlSigningService xmlSigner,
        IStorageService storageService,
        ILogger<DespatchAdviceService> logger)
    {
        _repository = repository;
        _tenantRepository = tenantRepository;
        _xmlBuilder = xmlBuilder;
        _greSunatClient = greSunatClient;
        _xmlSigner = xmlSigner;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<DespatchAdviceResponse> CreateAsync(CreateDespatchAdviceRequest request, Guid tenantId, CancellationToken ct = default)
    {
        var correlative = await _repository.GetNextCorrelativeAsync(tenantId, request.Serie, ct);

        var entity = new DespatchAdvice
        {
            TenantId = tenantId,
            DocumentType = request.DocumentType,
            Serie = request.Serie,
            Correlative = correlative,
            IssueDate = request.IssueDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            IssueTime = request.IssueTime ?? TimeOnly.FromDateTime(DateTime.UtcNow),
            TransferStartDate = request.TransferStartDate,
            TransferReasonCode = request.TransferReasonCode,
            TransferReasonDescription = request.TransferReasonDescription,
            Note = request.Note,
            GrossWeight = request.GrossWeight,
            WeightUnitCode = request.WeightUnitCode,
            TotalPackages = request.TotalPackages,
            TransportMode = request.TransportMode,
            CarrierDocType = request.CarrierDocType,
            CarrierDocNumber = request.CarrierDocNumber,
            CarrierName = request.CarrierName,
            CarrierMtcNumber = request.CarrierMtcNumber,
            DriverDocType = request.DriverDocType,
            DriverDocNumber = request.DriverDocNumber,
            DriverName = request.DriverName,
            DriverLicense = request.DriverLicense,
            VehiclePlate = request.VehiclePlate,
            SecondaryVehiclePlate = request.SecondaryVehiclePlate,
            RecipientDocType = request.RecipientDocType,
            RecipientDocNumber = request.RecipientDocNumber,
            RecipientName = request.RecipientName,
            OriginUbigeo = request.OriginUbigeo,
            OriginAddress = request.OriginAddress,
            DestinationUbigeo = request.DestinationUbigeo,
            DestinationAddress = request.DestinationAddress,
            RelatedDocType = request.RelatedDocType,
            RelatedDocNumber = request.RelatedDocNumber,
            Status = "draft",
            Items = request.Items.Select((item, idx) => new DespatchAdviceItem
            {
                LineNumber = idx + 1,
                Description = item.Description,
                ProductCode = item.ProductCode,
                Quantity = item.Quantity,
                UnitCode = item.UnitCode
            }).ToList()
        };

        await _repository.AddAsync(entity, ct);
        _logger.LogInformation("DespatchAdvice created: {FullNumber}", entity.FullNumber);

        return MapToResponse(entity);
    }

    public async Task<DespatchAdviceResponse> EmitAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdWithItemsAsync(id, ct)
            ?? throw new InvalidOperationException($"GRE {id} no encontrada");

        if (entity.TenantId != tenantId)
            throw new InvalidOperationException("GRE no pertenece a este tenant");

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Tenant no encontrado");

        // 1. Build XML
        var xml = _xmlBuilder.BuildDespatchAdviceXml(entity, tenant);

        // 2. Sign XML (synchronous — needs certificate data + password)
        // TODO: Load real certificate from TenantServiceConfig
        var certData = Array.Empty<byte>(); // placeholder until cert management is wired
        var certPassword = "";
        var (signedXml, digestValue) = _xmlSigner.SignXml(xml, certData, certPassword);

        // 3. Create ZIP
        var xmlFileName = $"{tenant.Ruc}-{entity.DocumentType}-{entity.FullNumber}.xml";
        var zipBytes = CreateZip(xmlFileName, signedXml);

        // 4. Store XML in MinIO
        entity.XmlUrl = await _storageService.UploadXmlAsync(
            tenantId, $"{entity.FullNumber}.xml",
            System.Text.Encoding.UTF8.GetBytes(signedXml), ct);

        // 5. Get OAuth2 token for GRE REST API
        if (string.IsNullOrEmpty(tenant.GreClientId) || string.IsNullOrEmpty(tenant.GreClientSecret))
            throw new InvalidOperationException("Credenciales GRE (client_id/client_secret) no configuradas. Generarlas en menú SOL de SUNAT.");
        if (string.IsNullOrEmpty(tenant.SunatUser) || string.IsNullOrEmpty(tenant.SunatPasswordEncrypted))
            throw new InvalidOperationException("Credenciales SOL no configuradas para este tenant.");

        var token = await _greSunatClient.GetTokenAsync(
            tenant.GreClientId, tenant.GreClientSecret,
            tenant.Ruc, tenant.SunatUser, tenant.SunatPasswordEncrypted, ct);

        // 6. Send to SUNAT via REST
        var response = await _greSunatClient.SendDespatchAdviceAsync(
            token, tenant.Ruc, entity.DocumentType,
            entity.Serie, entity.Correlative, zipBytes, ct);

        // 7. Update status
        entity.Status = response.Success ? "sent" : "rejected";
        entity.SunatTicket = response.Ticket;
        entity.SunatResponseCode = response.ResponseCode;
        entity.SunatResponseMessage = response.Description;

        if (response.CdrZip is not null)
        {
            entity.CdrUrl = await _storageService.UploadCdrAsync(
                tenantId, $"{entity.FullNumber}-cdr.zip", response.CdrZip, ct);
            entity.Status = "accepted";
        }

        await _repository.UpdateAsync(entity, ct);
        _logger.LogInformation("DespatchAdvice emitted: {FullNumber} Status: {Status}",
            entity.FullNumber, entity.Status);

        return MapToResponse(entity);
    }

    public async Task<DespatchAdviceResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdWithItemsAsync(id, ct);
        return entity is null ? null : MapToResponse(entity);
    }

    public async Task<(IReadOnlyList<DespatchAdviceResponse> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? documentType = null, string? status = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.ListAsync(
            tenantId, page, pageSize, documentType, status, dateFrom, dateTo, ct);

        return (items.Select(MapToResponse).ToList(), totalCount);
    }

    private static DespatchAdviceResponse MapToResponse(DespatchAdvice da)
    {
        return new DespatchAdviceResponse(
            da.Id,
            da.DocumentType,
            da.DocumentType == "09" ? "GRE Remitente" : "GRE Transportista",
            da.Serie,
            da.Correlative,
            da.FullNumber,
            da.IssueDate,
            da.IssueTime,
            da.TransferStartDate,
            da.TransferReasonCode,
            da.TransferReasonDescription,
            da.Note,
            da.GrossWeight,
            da.WeightUnitCode,
            da.TotalPackages,
            da.TransportMode,
            da.TransportMode == "01" ? "Transporte Público" : "Transporte Privado",
            da.CarrierDocNumber,
            da.CarrierName,
            da.DriverDocNumber,
            da.DriverName,
            da.DriverLicense,
            da.VehiclePlate,
            da.RecipientDocType,
            da.RecipientDocNumber,
            da.RecipientName,
            da.OriginUbigeo,
            da.OriginAddress,
            da.DestinationUbigeo,
            da.DestinationAddress,
            da.RelatedDocType,
            da.RelatedDocNumber,
            da.Status,
            da.SunatResponseCode,
            da.SunatResponseMessage,
            da.SunatTicket,
            da.XmlUrl,
            da.PdfUrl,
            da.CreatedAt,
            da.Items.OrderBy(i => i.LineNumber).Select(i => new DespatchAdviceItemResponse(
                i.LineNumber,
                i.Description,
                i.ProductCode,
                i.Quantity,
                i.UnitCode
            )).ToList()
        );
    }

    private static byte[] CreateZip(string fileName, string xmlContent)
    {
        using var ms = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry(fileName);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(xmlContent);
        }
        return ms.ToArray();
    }
}
