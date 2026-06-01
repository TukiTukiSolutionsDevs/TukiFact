using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;
using TukiFact.Application.DTOs.Documents;
using TukiFact.Application.Exceptions;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Enums;
using TukiFact.Domain.Services;

namespace TukiFact.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepo;
    private readonly ISeriesRepository _seriesRepo;
    private readonly ITenantRepository _tenantRepo;
    private readonly IUblBuilder _ublBuilder;
    private readonly IXmlSigningService _signingService;
    private readonly ISunatClient _sunatClient;
    private readonly IStorageService _storageService;
    private readonly IPdfGenerator _pdfGenerator;
    private readonly ISecretProtector _secrets;
    private readonly IEventPublisher _eventPublisher;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<DocumentService> _logger;
    private const decimal IgvRate = 0.18m;

    public DocumentService(
        IDocumentRepository documentRepo,
        ISeriesRepository seriesRepo,
        ITenantRepository tenantRepo,
        IUblBuilder ublBuilder,
        IXmlSigningService signingService,
        ISunatClient sunatClient,
        IStorageService storageService,
        IPdfGenerator pdfGenerator,
        ISecretProtector secrets,
        IEventPublisher eventPublisher,
        IExchangeRateService exchangeRateService,
        ILogger<DocumentService> logger)
    {
        _documentRepo = documentRepo;
        _seriesRepo = seriesRepo;
        _tenantRepo = tenantRepo;
        _ublBuilder = ublBuilder;
        _signingService = signingService;
        _sunatClient = sunatClient;
        _storageService = storageService;
        _pdfGenerator = pdfGenerator;
        _secrets = secrets;
        _eventPublisher = eventPublisher;
        _exchangeRateService = exchangeRateService;
        _logger = logger;
    }

    /// <summary>
    /// Generate the PDF once at acceptance time and persist it in MinIO. Avoids re-rendering
    /// on every GET /documents/{id}/pdf (CPU/RAM win for large item counts). Best-effort —
    /// a PDF render failure does NOT roll back the emission, since the doc is already in SUNAT.
    /// </summary>
    private async Task EnsurePdfPersistedAsync(Document doc, Tenant tenant, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(doc.PdfUrl)) return;
        try
        {
            var pdfBytes = _pdfGenerator.GenerateInvoicePdf(doc, tenant);
            doc.PdfUrl = await _storageService.UploadPdfAsync(tenant.Id, $"{doc.FullNumber}.pdf", pdfBytes, ct);
            await _documentRepo.UpdateAsync(doc, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF persist failed for {FullNumber} (will fall back to on-demand render)", doc.FullNumber);
        }
    }

    /// <summary>
    /// Persist SBS sell rate on USD invoices so PLE 14.1 reconciliation uses the rate
    /// of the issue date, not whatever rate happens to be cached at report time.
    /// Fail-fast: a missing rate means we cannot legally emit a USD doc.
    /// </summary>
    private async Task EnforcePlanLimitAsync(Tenant tenant, CancellationToken ct)
    {
        // Tenant without a plan = treat as Free defaults (matches DataSeeder's Free plan
        // and the GetTenant response's "Free / 50" fallback).
        var planName = tenant.Plan?.Name ?? "Gratis";
        var monthlyLimit = tenant.Plan?.MaxDocumentsPerMonth ?? 50;

        var count = await _documentRepo.CountForCurrentMonthAsync(tenant.Id, ct);
        if (count >= monthlyLimit)
        {
            _logger.LogWarning(
                "Tenant {TenantId} exceeded plan limit: {Count}/{Limit} ({Plan})",
                tenant.Id, count, monthlyLimit, planName);
            throw new PlanLimitExceededException(planName, monthlyLimit, count);
        }
    }

    private async Task EnsureExchangeRateAsync(Document doc, CancellationToken ct)
    {
        if (!string.Equals(doc.Currency, "USD", StringComparison.OrdinalIgnoreCase))
            return;

        var rate = await _exchangeRateService.GetRateAsync(doc.IssueDate, "USD", ct);
        if (rate is null)
        {
            // GetRateAsync falls back to FetchAndSave when missing; if it still returned null
            // SBS itself is unreachable or doesn't publish a rate for this date.
            throw new InvalidOperationException(
                $"No se pudo obtener el tipo de cambio SBS USD para {doc.IssueDate:yyyy-MM-dd}. " +
                "Reintentá cuando SBS publique el TC o emití en PEN.");
        }

        doc.ExchangeRate = rate.SellRate;
        doc.ExchangeRateDate = rate.Date;
    }

    /// <summary>
    /// Publish a domain event after a document reaches a terminal/notifiable state.
    /// Subjects match what existing handlers (NotificationEventHandler, GenericWebhookHandler) listen for.
    /// Failures here are logged but NEVER bubble — emission must not roll back on a publish hiccup.
    /// </summary>
    private async Task PublishDocumentEventAsync(Document doc, Tenant tenant, CancellationToken ct)
    {
        var subject = doc.Status switch
        {
            DocumentStatus.Accepted => "document.sent",
            DocumentStatus.Sent     => "document.sent",
            DocumentStatus.Rejected => "document.failed",
            _ => null
        };
        if (subject is null) return;

        var evt = new EventHandlers.TukiFactEvent
        {
            TenantId = tenant.Id,
            EntityId = doc.Id,
            EntityType = "Document",
            EventType = subject,
            DocumentType = doc.DocumentType,
            Serie = doc.Serie,
            Correlative = doc.Correlative,
            FullNumber = doc.FullNumber,
            Total = doc.Total,
            Currency = doc.Currency,
            Status = doc.Status,
            CustomerName = doc.CustomerName,
            CustomerEmail = doc.CustomerEmail,
            SunatResponseCode = doc.SunatResponseCode,
            SunatResponseDescription = doc.SunatResponseDescription
        };

        try
        {
            await _eventPublisher.PublishAsync(subject, evt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Publish {Subject} failed for {FullNumber}", subject, doc.FullNumber);
        }
    }

    public async Task<DocumentResponse> EmitAsync(CreateDocumentRequest request, Guid tenantId, CancellationToken ct = default)
    {
        // 1. Validate and get tenant
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Tenant no encontrado");

        // Validate items
        if (request.Items is null || request.Items.Count == 0)
            throw new ArgumentException("El documento debe contener al menos un item en la lista 'items'");

        // 1b. Plan limit — block emit BEFORE consuming a correlative; otherwise we'd
        // burn the next number on a doc that never gets created.
        await EnforcePlanLimitAsync(tenant, ct);

        // 2. Get series and next correlative
        var series = await _seriesRepo.GetByTypeAndSerieAsync(tenantId, request.DocumentType, request.Serie, ct)
            ?? throw new InvalidOperationException($"Serie {request.Serie} no encontrada para tipo {request.DocumentType}");

        if (!series.IsActive)
            throw new InvalidOperationException($"La serie '{request.Serie}' está inactiva.");

        var correlative = await _seriesRepo.GetNextCorrelativeAsync(series.Id, ct);

        // 3. Build document with calculated amounts
        var document = BuildDocument(request, tenantId, series, correlative);

        // 3b. Lock in SBS sell rate for USD docs (fail-fast if SBS unreachable)
        await EnsureExchangeRateAsync(document, ct);

        // 4. Save to DB
        await _documentRepo.CreateAsync(document, ct);
        _logger.LogInformation("Document created: {FullNumber}", document.FullNumber);

        // 5. Build UBL XML
        var xml = _ublBuilder.BuildInvoiceXml(document, tenant);

        // 6. Sign XML — required; an unsigned XML to SUNAT = guaranteed rejection 0306 + masks the failure.
        if (tenant.CertificateData is null || tenant.CertificatePasswordEncrypted is null)
        {
            document.Status = DocumentStatus.Rejected;
            document.SunatResponseDescription = "El emisor no tiene certificado digital configurado.";
            await _documentRepo.UpdateAsync(document, ct);
            throw new InvalidOperationException("El emisor no tiene certificado digital configurado.");
        }

        string signedXml;
        string hashCode;
        try
        {
            var (signed, digest) = _signingService.SignXml(xml, tenant.CertificateData, _secrets.Unprotect(tenant.CertificatePasswordEncrypted));
            signedXml = signed;
            hashCode = digest;
            document.HashCode = hashCode;
            document.Status = DocumentStatus.Signed;
            _logger.LogInformation("Document signed: {FullNumber}, Hash: {Hash}", document.FullNumber, hashCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign document {FullNumber}.", document.FullNumber);
            document.Status = DocumentStatus.Rejected;
            document.SunatResponseDescription = "Error firmando XML: " + ex.Message;
            await _documentRepo.UpdateAsync(document, ct);
            throw new InvalidOperationException("Error firmando XML: " + ex.Message);
        }

        // 7. Store XML in MinIO
        var xmlBytes = Encoding.UTF8.GetBytes(signedXml);
        var xmlPath = await _storageService.UploadXmlAsync(tenantId,
            $"{document.FullNumber}.xml", xmlBytes, ct);
        document.XmlUrl = xmlPath;

        // 7b. Atomic materialization checkpoint (C7): persist HashCode + XmlUrl + Status=Signed
        // BEFORE calling SUNAT. If the process dies during/after the SOAP call we still have
        // a record that distinguishes "consumed correlative but never reached SUNAT" from
        // "in-flight to SUNAT but response not yet persisted". Recovery worker scans for this.
        await _documentRepo.UpdateAsync(document, ct);

        // 8. Send to SUNAT — credentials per tenant (NO global fallback)
        if (string.IsNullOrEmpty(tenant.SunatUser) || string.IsNullOrEmpty(tenant.SunatPasswordEncrypted))
        {
            document.Status = DocumentStatus.Rejected;
            document.SunatResponseDescription = "Faltan credenciales SUNAT (SOL user/password) en la configuración del emisor.";
            await _documentRepo.UpdateAsync(document, ct);
            throw new InvalidOperationException("Faltan credenciales SUNAT (SOL) en la configuración del emisor.");
        }
        var sunatCreds = new SunatCredentials(tenant.SunatUser, _secrets.Unprotect(tenant.SunatPasswordEncrypted), tenant.Environment);
        try
        {
            var zipBytes = CreateZipFromXml($"{tenant.Ruc}-{document.DocumentType}-{document.FullNumber}.xml", xmlBytes);
            var sunatResponse = await _sunatClient.SendDocumentAsync(
                tenant.Ruc, document.DocumentType, document.FullNumber, zipBytes, sunatCreds, ct);

            document.SunatResponseCode = sunatResponse.ResponseCode;
            document.SunatResponseDescription = sunatResponse.Description;

            if (sunatResponse.Success)
            {
                document.Status = DocumentStatus.Accepted;
                if (sunatResponse.CdrZip is not null)
                {
                    var cdrPath = await _storageService.UploadCdrAsync(tenantId,
                        $"R-{document.FullNumber}.zip", sunatResponse.CdrZip, ct);
                    document.CdrUrl = cdrPath;
                }
            }
            else
            {
                document.Status = DocumentStatus.Rejected;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send document {FullNumber} to SUNAT", document.FullNumber);
            document.Status = DocumentStatus.Sent; // Mark as sent but response pending
        }

        // 9. Generate QR
        document.QrData = $"{tenant.Ruc}|{document.DocumentType}|{document.Serie}|{document.Correlative}|{document.Igv:F2}|{document.Total:F2}|{document.IssueDate:yyyy-MM-dd}|{document.CustomerDocType}|{document.CustomerDocNumber}|{hashCode ?? ""}";

        // 10. Update document
        await _documentRepo.UpdateAsync(document, ct);

        if (document.Status == DocumentStatus.Accepted)
            await EnsurePdfPersistedAsync(document, tenant, ct);

        await PublishDocumentEventAsync(document, tenant, ct);

        return MapToResponse(document);
    }

    public async Task<DocumentResponse?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var doc = await _documentRepo.GetByIdWithItemsAsync(id, tenantId, ct);
        return doc is null ? null : MapToResponse(doc);
    }

    public async Task<(IReadOnlyList<DocumentResponse> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize,
        string? documentType = null, string? status = null,
        DateOnly? dateFrom = null, DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var (docs, total) = await _documentRepo.GetByTenantAsync(
            tenantId, page, pageSize, documentType, status, dateFrom, dateTo, ct);
        return (docs.Select(MapToResponse).ToList(), total);
    }

    private Document BuildDocument(CreateDocumentRequest request, Guid tenantId, Series series, long correlative)
    {
        var document = new Document
        {
            TenantId = tenantId,
            DocumentType = request.DocumentType,
            SeriesId = series.Id,
            Serie = request.Serie,
            Correlative = correlative,
            IssueDate = request.IssueDate ?? RecurringScheduleCalculator.TodayInLima(),
            DueDate = request.DueDate,
            Currency = request.Currency ?? "PEN",
            CustomerDocType = request.CustomerDocType,
            CustomerDocNumber = request.CustomerDocNumber,
            CustomerName = request.CustomerName,
            CustomerAddress = request.CustomerAddress,
            CustomerEmail = request.CustomerEmail,
            Notes = request.Notes,
            PurchaseOrder = request.PurchaseOrder
        };

        // Build items and calculate totals
        decimal totalGravada = 0, totalExonerada = 0, totalInafecta = 0, totalGratuita = 0;
        decimal totalIgv = 0, totalDescuento = 0;
        int seq = 1;

        foreach (var itemReq in request.Items)
        {
            var subtotal = Math.Round(itemReq.Quantity * itemReq.UnitPrice, 2);
            var discount = Math.Round(itemReq.Discount, 2);
            var taxableAmount = subtotal - discount;

            decimal igvAmount = 0;
            decimal unitPriceWithIgv = itemReq.UnitPrice;

            if (itemReq.IgvType == "10") // Gravado
            {
                igvAmount = Math.Round(taxableAmount * IgvRate, 2);
                unitPriceWithIgv = Math.Round(itemReq.UnitPrice * (1 + IgvRate), 4);
                totalGravada += taxableAmount;
            }
            else if (itemReq.IgvType == "20") // Exonerado
            {
                totalExonerada += taxableAmount;
            }
            else if (itemReq.IgvType == "30") // Inafecto
            {
                totalInafecta += taxableAmount;
            }
            else if (itemReq.IgvType == "21") // Gratuito
            {
                totalGratuita += taxableAmount;
            }

            totalIgv += igvAmount;
            totalDescuento += discount;

            var item = new DocumentItem
            {
                Sequence = seq++,
                ProductCode = itemReq.ProductCode,
                SunatProductCode = itemReq.SunatProductCode,
                Description = itemReq.Description,
                Quantity = itemReq.Quantity,
                UnitMeasure = itemReq.UnitMeasure ?? "NIU",
                UnitPrice = itemReq.UnitPrice,
                UnitPriceWithIgv = unitPriceWithIgv,
                IgvType = itemReq.IgvType,
                IgvAmount = igvAmount,
                Subtotal = taxableAmount,
                Discount = discount,
                Total = taxableAmount + igvAmount
            };
            document.Items.Add(item);
        }

        document.OperacionGravada = Math.Round(totalGravada, 2);
        document.OperacionExonerada = Math.Round(totalExonerada, 2);
        document.OperacionInafecta = Math.Round(totalInafecta, 2);
        document.OperacionGratuita = Math.Round(totalGratuita, 2);
        document.Igv = Math.Round(totalIgv, 2);
        document.TotalDescuento = Math.Round(totalDescuento, 2);
        document.Total = Math.Round(totalGravada + totalExonerada + totalInafecta + totalIgv, 2);

        return document;
    }

    private static byte[] CreateZipFromXml(string fileName, byte[] xmlBytes)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var entry = zip.CreateEntry(fileName, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(xmlBytes);
        }
        return ms.ToArray();
    }

    public async Task<DocumentResponse> EmitCreditNoteAsync(CreateCreditNoteRequest request, Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Tenant no encontrado");

        await EnforcePlanLimitAsync(tenant, ct);

        // Get the reference document (tenant-scoped — prevents IDOR across tenants)
        var refDoc = await _documentRepo.GetByIdWithItemsAsync(request.ReferenceDocumentId, tenantId, ct)
            ?? throw new InvalidOperationException("Documento de referencia no encontrado");

        // SUNAT business rules — run BEFORE consuming a correlativo so a bad request doesn't burn one.
        if (refDoc.DocumentType != "01" && refDoc.DocumentType != "03")
            throw new InvalidOperationException($"Solo puedes emitir NC sobre Factura (01) o Boleta (03). El documento de referencia es tipo {refDoc.DocumentType}.");

        if (refDoc.Status != DocumentStatus.Accepted)
            throw new InvalidOperationException($"Solo puedes emitir NC sobre documentos aceptados por SUNAT. El documento de referencia está en estado '{refDoc.Status}'.");

        if (!string.Equals(refDoc.Currency, request.Currency, StringComparison.Ordinal))
            throw new InvalidOperationException($"La moneda de la NC ({request.Currency}) debe coincidir con la del documento de referencia ({refDoc.Currency}).");

        if (request.Serie.Length > 0 && refDoc.Serie.Length > 0 && request.Serie[0] != refDoc.Serie[0])
            throw new InvalidOperationException($"La serie '{request.Serie}' no coincide con el prefijo del documento de referencia ('{refDoc.Serie}'). Usa una serie con prefijo '{refDoc.Serie[0]}'.");

        // Get series for the credit note (type 07)
        var series = await _seriesRepo.GetByTypeAndSerieAsync(tenantId, "07", request.Serie, ct)
            ?? throw new InvalidOperationException($"Serie {request.Serie} no encontrada para Nota de Crédito");

        if (!series.IsActive)
            throw new InvalidOperationException($"La serie '{request.Serie}' está inactiva.");

        var correlative = await _seriesRepo.GetNextCorrelativeAsync(series.Id, ct);

        var document = BuildDocumentFromRequest(new CreateDocumentRequest(
            "07", request.Serie, null, null, request.Currency,
            refDoc.CustomerDocType, refDoc.CustomerDocNumber, refDoc.CustomerName,
            refDoc.CustomerAddress, null, request.Description, null, request.Items),
            tenantId, series, correlative);

        document.ReferenceDocumentId = request.ReferenceDocumentId;
        document.ReferenceDocumentType = refDoc.DocumentType;
        document.ReferenceDocumentNumber = refDoc.FullNumber;
        document.CreditNoteReason = request.CreditNoteReason;

        await EnsureExchangeRateAsync(document, ct);

        await _documentRepo.CreateAsync(document, ct);
        _logger.LogInformation("CreditNote created: {FullNumber} for {RefNumber}", document.FullNumber, refDoc.FullNumber);

        return await ProcessAndSendDocument(document, tenant, ct);
    }

    public async Task<DocumentResponse> EmitDebitNoteAsync(CreateDebitNoteRequest request, Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Tenant no encontrado");

        await EnforcePlanLimitAsync(tenant, ct);

        var refDoc = await _documentRepo.GetByIdWithItemsAsync(request.ReferenceDocumentId, tenantId, ct)
            ?? throw new InvalidOperationException("Documento de referencia no encontrado");

        // SUNAT business rules — same set as NC; ND only valid against accepted Factura/Boleta.
        if (refDoc.DocumentType != "01" && refDoc.DocumentType != "03")
            throw new InvalidOperationException($"Solo puedes emitir ND sobre Factura (01) o Boleta (03). El documento de referencia es tipo {refDoc.DocumentType}.");

        if (refDoc.Status != DocumentStatus.Accepted)
            throw new InvalidOperationException($"Solo puedes emitir ND sobre documentos aceptados por SUNAT. El documento de referencia está en estado '{refDoc.Status}'.");

        if (!string.Equals(refDoc.Currency, request.Currency, StringComparison.Ordinal))
            throw new InvalidOperationException($"La moneda de la ND ({request.Currency}) debe coincidir con la del documento de referencia ({refDoc.Currency}).");

        if (request.Serie.Length > 0 && refDoc.Serie.Length > 0 && request.Serie[0] != refDoc.Serie[0])
            throw new InvalidOperationException($"La serie '{request.Serie}' no coincide con el prefijo del documento de referencia ('{refDoc.Serie}'). Usa una serie con prefijo '{refDoc.Serie[0]}'.");

        var series = await _seriesRepo.GetByTypeAndSerieAsync(tenantId, "08", request.Serie, ct)
            ?? throw new InvalidOperationException($"Serie {request.Serie} no encontrada para Nota de Débito");

        if (!series.IsActive)
            throw new InvalidOperationException($"La serie '{request.Serie}' está inactiva.");

        var correlative = await _seriesRepo.GetNextCorrelativeAsync(series.Id, ct);

        var document = BuildDocumentFromRequest(new CreateDocumentRequest(
            "08", request.Serie, null, null, request.Currency,
            refDoc.CustomerDocType, refDoc.CustomerDocNumber, refDoc.CustomerName,
            refDoc.CustomerAddress, null, request.Description, null, request.Items),
            tenantId, series, correlative);

        document.ReferenceDocumentId = request.ReferenceDocumentId;
        document.ReferenceDocumentType = refDoc.DocumentType;
        document.ReferenceDocumentNumber = refDoc.FullNumber;
        document.DebitNoteReason = request.DebitNoteReason;

        await EnsureExchangeRateAsync(document, ct);

        await _documentRepo.CreateAsync(document, ct);
        _logger.LogInformation("DebitNote created: {FullNumber} for {RefNumber}", document.FullNumber, refDoc.FullNumber);

        return await ProcessAndSendDocument(document, tenant, ct);
    }

    private async Task<DocumentResponse> ProcessAndSendDocument(Document document, Tenant tenant, CancellationToken ct)
    {
        // Build UBL XML based on document type
        var xml = document.DocumentType switch
        {
            "07" => _ublBuilder.BuildCreditNoteXml(document, tenant),
            "08" => _ublBuilder.BuildDebitNoteXml(document, tenant),
            _ => _ublBuilder.BuildInvoiceXml(document, tenant)
        };

        // Sign XML — required; an unsigned XML to SUNAT = guaranteed rejection 0306.
        if (tenant.CertificateData is null || tenant.CertificatePasswordEncrypted is null)
        {
            document.Status = DocumentStatus.Rejected;
            document.SunatResponseDescription = "El emisor no tiene certificado digital configurado.";
            await _documentRepo.UpdateAsync(document, ct);
            throw new InvalidOperationException("El emisor no tiene certificado digital configurado.");
        }

        string signedXml;
        string hashCode;
        try
        {
            var (signed, digest) = _signingService.SignXml(xml, tenant.CertificateData, _secrets.Unprotect(tenant.CertificatePasswordEncrypted));
            signedXml = signed;
            hashCode = digest;
            document.HashCode = hashCode;
            document.Status = DocumentStatus.Signed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign document {FullNumber}.", document.FullNumber);
            document.Status = DocumentStatus.Rejected;
            document.SunatResponseDescription = "Error firmando XML: " + ex.Message;
            await _documentRepo.UpdateAsync(document, ct);
            throw new InvalidOperationException("Error firmando XML: " + ex.Message);
        }

        // Store XML
        var xmlBytes = Encoding.UTF8.GetBytes(signedXml);
        var xmlPath = await _storageService.UploadXmlAsync(tenant.Id,
            $"{document.FullNumber}.xml", xmlBytes, ct);
        document.XmlUrl = xmlPath;

        // Atomic materialization checkpoint (C7): persist HashCode + XmlUrl + Status=Signed
        // BEFORE the SUNAT call. See EmissionRecoveryHostedService for the recovery scan.
        await _documentRepo.UpdateAsync(document, ct);

        // Send to SUNAT — credentials per tenant (NO global fallback)
        if (string.IsNullOrEmpty(tenant.SunatUser) || string.IsNullOrEmpty(tenant.SunatPasswordEncrypted))
        {
            document.Status = DocumentStatus.Rejected;
            document.SunatResponseDescription = "Faltan credenciales SUNAT (SOL user/password) en la configuración del emisor.";
            await _documentRepo.UpdateAsync(document, ct);
            throw new InvalidOperationException("Faltan credenciales SUNAT (SOL) en la configuración del emisor.");
        }
        var sunatCreds = new SunatCredentials(tenant.SunatUser, _secrets.Unprotect(tenant.SunatPasswordEncrypted), tenant.Environment);
        try
        {
            var zipBytes = CreateZipFromXml($"{tenant.Ruc}-{document.DocumentType}-{document.FullNumber}.xml", xmlBytes);
            var sunatResponse = await _sunatClient.SendDocumentAsync(
                tenant.Ruc, document.DocumentType, document.FullNumber, zipBytes, sunatCreds, ct);

            document.SunatResponseCode = sunatResponse.ResponseCode;
            document.SunatResponseDescription = sunatResponse.Description;

            if (sunatResponse.Success)
            {
                document.Status = DocumentStatus.Accepted;
                if (sunatResponse.CdrZip is not null)
                {
                    var cdrPath = await _storageService.UploadCdrAsync(tenant.Id,
                        $"R-{document.FullNumber}.zip", sunatResponse.CdrZip, ct);
                    document.CdrUrl = cdrPath;
                }
            }
            else
            {
                document.Status = DocumentStatus.Rejected;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send document {FullNumber} to SUNAT", document.FullNumber);
            document.Status = DocumentStatus.Sent;
        }

        document.QrData = $"{tenant.Ruc}|{document.DocumentType}|{document.Serie}|{document.Correlative}|{document.Igv:F2}|{document.Total:F2}|{document.IssueDate:yyyy-MM-dd}|{document.CustomerDocType}|{document.CustomerDocNumber}|{hashCode}";

        await _documentRepo.UpdateAsync(document, ct);

        if (document.Status == DocumentStatus.Accepted)
            await EnsurePdfPersistedAsync(document, tenant, ct);

        await PublishDocumentEventAsync(document, tenant, ct);

        return MapToResponse(document);
    }

    private Document BuildDocumentFromRequest(CreateDocumentRequest request, Guid tenantId, Series series, long correlative)
        => BuildDocument(request, tenantId, series, correlative);

    private static DocumentResponse MapToResponse(Document doc)
    {
        return new DocumentResponse(
            doc.Id, doc.DocumentType, DocumentType.GetName(doc.DocumentType),
            doc.Serie, doc.Correlative, doc.FullNumber,
            doc.IssueDate, doc.DueDate, doc.Currency,
            doc.CustomerDocType, doc.CustomerDocNumber, doc.CustomerName,
            doc.OperacionGravada, doc.OperacionExonerada, doc.OperacionInafecta,
            doc.OperacionGravada + doc.OperacionExonerada + doc.OperacionInafecta,
            doc.Igv, doc.Total, doc.Status,
            doc.SunatResponseCode, doc.SunatResponseDescription,
            doc.HashCode, doc.XmlUrl, doc.PdfUrl, doc.Notes, doc.CreatedAt,
            doc.Items.OrderBy(i => i.Sequence).Select(i => new DocumentItemResponse(
                i.Sequence, i.ProductCode, i.Description, i.Quantity,
                i.UnitMeasure, i.UnitPrice, i.UnitPriceWithIgv,
                i.IgvType, i.IgvAmount, i.Subtotal, i.Total)).ToList());
    }
}
