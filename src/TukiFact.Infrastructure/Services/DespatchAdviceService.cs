using Microsoft.Extensions.Logging;
using TukiFact.Application.DTOs.DespatchAdvices;
using TukiFact.Application.Interfaces;
using TukiFact.Application.Validation;
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
    private readonly IAuditLogRepository _auditLog;
    private readonly ISecretProtector _secrets;
    private readonly IPdfGenerator _pdfGenerator;
    private readonly ILogger<DespatchAdviceService> _logger;

    // Polling configuration (DESIGN-CLIENT §10 / audit gap #2)
    private static readonly TimeSpan[] PollingDelays =
    {
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(3),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(8),
        TimeSpan.FromSeconds(13),
    };

    public DespatchAdviceService(
        IDespatchAdviceRepository repository,
        ITenantRepository tenantRepository,
        IGreXmlBuilder xmlBuilder,
        IGreSunatClient greSunatClient,
        IXmlSigningService xmlSigner,
        IStorageService storageService,
        IAuditLogRepository auditLog,
        ISecretProtector secrets,
        IPdfGenerator pdfGenerator,
        ILogger<DespatchAdviceService> logger)
    {
        _repository = repository;
        _tenantRepository = tenantRepository;
        _xmlBuilder = xmlBuilder;
        _greSunatClient = greSunatClient;
        _xmlSigner = xmlSigner;
        _storageService = storageService;
        _auditLog = auditLog;
        _secrets = secrets;
        _pdfGenerator = pdfGenerator;
        _logger = logger;
    }

    public async Task<DespatchAdviceResponse> CreateAsync(CreateDespatchAdviceRequest request, Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var errors = DespatchAdviceValidator.Validate(request);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" · ", errors));

        var correlative = await _repository.GetNextCorrelativeAsync(tenantId, request.Serie, ct);

        var entity = new DespatchAdvice
        {
            TenantId = tenantId,
            CreatedByUserId = userId,
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
            // Default to KGM (kilogramos) if the client didn't send a unit code (audit gap #4).
            WeightUnitCode = string.IsNullOrWhiteSpace(request.WeightUnitCode) ? "KGM" : request.WeightUnitCode,
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

        await WriteAuditAsync(
            tenantId, userId, "despatch.created", entity.Id,
            $"{{\"fullNumber\":\"{entity.FullNumber}\",\"recipient\":\"{Escape(entity.RecipientName)}\"}}", ct);

        return MapToResponse(entity);
    }

    public async Task<DespatchAdviceResponse> EmitAsync(Guid id, Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdWithItemsAsync(id, ct)
            ?? throw new InvalidOperationException($"GRE {id} no encontrada");

        if (entity.TenantId != tenantId)
            throw new InvalidOperationException("GRE no pertenece a este tenant");

        if (entity.Status != "draft")
            throw new InvalidOperationException(
                $"Solo se pueden emitir guías en estado borrador. Estado actual: {entity.Status}");

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Tenant no encontrado");

        // Validate the tenant has everything we need before doing any expensive work (audit gap #1).
        EnsureSigningRequirements(tenant);
        EnsureSunatCredentials(tenant);

        // 1. Build XML
        var xml = _xmlBuilder.BuildDespatchAdviceXml(entity, tenant);

        // 2. Sign XML with the tenant's certificate.
        var certPassword = _secrets.Unprotect(tenant.CertificatePasswordEncrypted);
        var (signedXml, _digest) = _xmlSigner.SignXml(xml, tenant.CertificateData!, certPassword);

        // 3. Create ZIP containing the signed XML.
        var xmlFileName = $"{tenant.Ruc}-{entity.DocumentType}-{entity.FullNumber}.xml";
        var zipBytes = CreateZip(xmlFileName, signedXml);

        // 4. Store the signed XML in MinIO for traceability.
        entity.XmlUrl = await _storageService.UploadXmlAsync(
            tenantId, $"{entity.FullNumber}.xml",
            System.Text.Encoding.UTF8.GetBytes(signedXml), ct);

        // 5. Get OAuth2 token for GRE REST API.
        var solPassword = _secrets.Unprotect(tenant.SunatPasswordEncrypted);
        var token = await _greSunatClient.GetTokenAsync(
            tenant.GreClientId!, tenant.GreClientSecret!,
            tenant.Ruc, tenant.SunatUser!, solPassword, ct);

        // 6. Send to SUNAT — returns a ticket; CDR is fetched asynchronously.
        var sendResponse = await _greSunatClient.SendDespatchAdviceAsync(
            token, tenant.Ruc, entity.DocumentType,
            entity.Serie, entity.Correlative, zipBytes, ct);

        entity.SunatTicket = sendResponse.Ticket;
        entity.SunatResponseCode = sendResponse.ResponseCode;
        entity.SunatResponseMessage = sendResponse.Description;

        if (!sendResponse.Success || string.IsNullOrEmpty(sendResponse.Ticket))
        {
            entity.Status = "rejected";
            await _repository.UpdateAsync(entity, ct);
            _logger.LogWarning(
                "GRE {FullNumber} rejected by SUNAT on send: {Code} {Message}",
                entity.FullNumber, sendResponse.ResponseCode, sendResponse.Description);

            await WriteAuditAsync(
                tenantId, userId, "despatch.rejected", entity.Id,
                $"{{\"fullNumber\":\"{entity.FullNumber}\",\"code\":\"{sendResponse.ResponseCode}\",\"message\":\"{Escape(sendResponse.Description ?? "")}\"}}", ct);

            return MapToResponse(entity);
        }

        entity.Status = "sent";

        // 7. Poll for ticket status (audit gap #2). SUNAT's GRE flow is asynchronous —
        // the CDR can take several seconds. If we time out here, the user can re-poll
        // via RefreshStatusAsync.
        await TryFetchCdrAsync(entity, tenant, token, ct);

        await _repository.UpdateAsync(entity, ct);
        _logger.LogInformation(
            "DespatchAdvice emitted: {FullNumber} Status: {Status} Ticket: {Ticket}",
            entity.FullNumber, entity.Status, entity.SunatTicket);

        await WriteAuditAsync(
            tenantId, userId, $"despatch.{entity.Status}", entity.Id,
            $"{{\"fullNumber\":\"{entity.FullNumber}\",\"ticket\":\"{entity.SunatTicket}\",\"code\":\"{entity.SunatResponseCode}\"}}", ct);

        return MapToResponse(entity);
    }

    public async Task<DespatchAdviceResponse> RefreshStatusAsync(Guid id, Guid tenantId, Guid userId, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdWithItemsAsync(id, ct)
            ?? throw new InvalidOperationException($"GRE {id} no encontrada");

        if (entity.TenantId != tenantId)
            throw new InvalidOperationException("GRE no pertenece a este tenant");

        if (string.IsNullOrEmpty(entity.SunatTicket))
            throw new InvalidOperationException(
                "Esta guía aún no tiene ticket de SUNAT. Emítela primero.");

        if (entity.Status is "accepted" or "rejected")
            return MapToResponse(entity); // Already final.

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Tenant no encontrado");

        EnsureSunatCredentials(tenant);

        var solPassword = _secrets.Unprotect(tenant.SunatPasswordEncrypted);
        var token = await _greSunatClient.GetTokenAsync(
            tenant.GreClientId!, tenant.GreClientSecret!,
            tenant.Ruc, tenant.SunatUser!, solPassword, ct);

        await TryFetchCdrAsync(entity, tenant, token, ct);
        await _repository.UpdateAsync(entity, ct);

        _logger.LogInformation(
            "DespatchAdvice {FullNumber} status refreshed: {Status}",
            entity.FullNumber, entity.Status);

        await WriteAuditAsync(
            tenantId, userId, "despatch.refreshed", entity.Id,
            $"{{\"fullNumber\":\"{entity.FullNumber}\",\"status\":\"{entity.Status}\",\"code\":\"{entity.SunatResponseCode}\"}}", ct);

        return MapToResponse(entity);
    }

    public async Task<DespatchAdviceResponse> CancelAsync(Guid id, Guid tenantId, Guid userId, string? reason, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdWithItemsAsync(id, ct)
            ?? throw new InvalidOperationException($"GRE {id} no encontrada");

        if (entity.TenantId != tenantId)
            throw new InvalidOperationException("GRE no pertenece a este tenant");

        if (entity.Status == "cancelled")
            throw new InvalidOperationException("Esta guía ya estaba anulada.");

        if (entity.Status == "draft")
        {
            throw new InvalidOperationException(
                "Esta guía es un borrador — no necesita anulación, basta con eliminarla o no emitirla.");
        }

        if (entity.Status == "rejected")
        {
            throw new InvalidOperationException(
                "Esta guía fue rechazada por SUNAT, ya no está vigente. No requiere anulación.");
        }

        var trimmedReason = string.IsNullOrWhiteSpace(reason)
            ? "Anulación registrada por el contribuyente"
            : reason.Trim();

        // TODO: send the formal Comunicación de Baja XML to SUNAT GRE REST. For now we register
        // the cancellation locally and let the user complete the formal flow via SOL portal if
        // needed. Once that flow is implemented, set status to "cancellation_pending" first and
        // flip to "cancelled" only after SUNAT acknowledges.
        entity.Status = "cancelled";
        entity.Note = string.IsNullOrEmpty(entity.Note)
            ? $"[ANULADA] {trimmedReason}"
            : entity.Note + $"\n[ANULADA] {trimmedReason}";

        await _repository.UpdateAsync(entity, ct);

        _logger.LogInformation(
            "GRE {FullNumber} cancelled locally by user {UserId}: {Reason}",
            entity.FullNumber, userId, trimmedReason);

        await WriteAuditAsync(
            tenantId, userId, "despatch.cancelled", entity.Id,
            $"{{\"fullNumber\":\"{entity.FullNumber}\",\"reason\":\"{Escape(trimmedReason)}\"}}", ct);

        return MapToResponse(entity);
    }

    public async Task<DespatchAdviceResponse?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdWithItemsAsync(id, ct);
        if (entity is null) return null;

        // Tenant isolation — never leak another tenant's data (audit gap #5: IDOR).
        if (entity.TenantId != tenantId) return null;

        return MapToResponse(entity);
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

    /// <summary>
    /// Poll SUNAT for the GRE ticket status with exponential back-off.
    /// On success: stores the CDR and updates the entity status to accepted/rejected.
    /// On timeout: entity stays in 'sent' and the caller can retry via RefreshStatusAsync.
    /// </summary>
    private async Task TryFetchCdrAsync(DespatchAdvice entity, Tenant tenant, string token, CancellationToken ct)
    {
        for (var attempt = 0; attempt < PollingDelays.Length; attempt++)
        {
            await Task.Delay(PollingDelays[attempt], ct);

            var statusResponse = await _greSunatClient.GetTicketStatusAsync(
                token, tenant.Ruc, entity.SunatTicket!, ct);

            entity.SunatResponseCode = statusResponse.ResponseCode ?? entity.SunatResponseCode;
            entity.SunatResponseMessage = statusResponse.Description ?? entity.SunatResponseMessage;

            if (statusResponse.CdrZip is not null)
            {
                entity.CdrUrl = await _storageService.UploadCdrAsync(
                    entity.TenantId, $"{entity.FullNumber}-cdr.zip", statusResponse.CdrZip, ct);
                entity.Status = statusResponse.Success ? "accepted" : "rejected";

                // Generate human-readable PDF for the GRE if accepted.
                if (entity.Status == "accepted")
                {
                    await TryGeneratePdfAsync(entity, tenant, ct);
                }

                _logger.LogInformation(
                    "GRE {FullNumber} CDR received on attempt {Attempt}: {Status}",
                    entity.FullNumber, attempt + 1, entity.Status);
                return;
            }

            // Definitive failure code from SUNAT — stop polling.
            if (statusResponse.Success == false && !string.IsNullOrEmpty(statusResponse.ResponseCode))
            {
                entity.Status = "rejected";
                _logger.LogWarning(
                    "GRE {FullNumber} rejected on attempt {Attempt}: {Code}",
                    entity.FullNumber, attempt + 1, statusResponse.ResponseCode);
                return;
            }
        }

        _logger.LogWarning(
            "GRE {FullNumber} polling timed out — status stays 'sent'. Use refresh-status to retry.",
            entity.FullNumber);
    }

    private static void EnsureSigningRequirements(Tenant tenant)
    {
        if (tenant.CertificateData is null || tenant.CertificateData.Length == 0)
            throw new InvalidOperationException(
                "No has subido tu certificado digital. Súbelo en Configuración → Certificado.");

        if (string.IsNullOrEmpty(tenant.CertificatePasswordEncrypted))
            throw new InvalidOperationException(
                "Falta la contraseña del certificado digital. Vuelve a subir el .pfx con su clave.");

        if (tenant.CertificateExpiresAt.HasValue && tenant.CertificateExpiresAt.Value < DateTimeOffset.UtcNow)
            throw new InvalidOperationException(
                $"Tu certificado digital expiró el {tenant.CertificateExpiresAt.Value:yyyy-MM-dd}. Renuévalo antes de emitir.");
    }

    private static void EnsureSunatCredentials(Tenant tenant)
    {
        if (string.IsNullOrEmpty(tenant.GreClientId) || string.IsNullOrEmpty(tenant.GreClientSecret))
            throw new InvalidOperationException(
                "Credenciales GRE (client_id/client_secret) no configuradas. Genéralas en menú SOL de SUNAT y guárdalas en Configuración → SUNAT.");

        if (string.IsNullOrEmpty(tenant.SunatUser) || string.IsNullOrEmpty(tenant.SunatPasswordEncrypted))
            throw new InvalidOperationException(
                "Credenciales SOL (usuario/clave SUNAT) no configuradas. Agrégalas en Configuración → SUNAT.");
    }

    private async Task TryGeneratePdfAsync(DespatchAdvice entity, Tenant tenant, CancellationToken ct)
    {
        try
        {
            var pdfBytes = _pdfGenerator.GenerateDespatchAdvicePdf(entity, tenant);
            entity.PdfUrl = await _storageService.UploadPdfAsync(
                entity.TenantId, $"{entity.FullNumber}.pdf", pdfBytes, ct);
            _logger.LogInformation("GRE {FullNumber} PDF generated and stored", entity.FullNumber);
        }
        catch (Exception ex)
        {
            // PDF failure shouldn't block the emit flow — log and carry on.
            _logger.LogWarning(ex, "Failed to generate/upload PDF for GRE {FullNumber}", entity.FullNumber);
        }
    }

    private async Task WriteAuditAsync(Guid tenantId, Guid userId, string action, Guid entityId, string detailsJson, CancellationToken ct)
    {
        try
        {
            await _auditLog.LogAsync(new Domain.Entities.AuditLog
            {
                TenantId = tenantId,
                UserId = userId == Guid.Empty ? null : userId,
                Action = action,
                EntityType = "DespatchAdvice",
                EntityId = entityId,
                Details = detailsJson,
                CreatedAt = DateTimeOffset.UtcNow,
            }, ct);
        }
        catch (Exception ex)
        {
            // Don't fail the operation if the audit log write fails — just record it.
            _logger.LogWarning(ex, "Failed to write audit log entry for {Action} {EntityId}", action, entityId);
        }
    }

    private static string Escape(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");

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
            da.CarrierDocType,
            da.CarrierDocNumber,
            da.CarrierName,
            da.DriverDocType,
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
            da.CdrUrl,
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
