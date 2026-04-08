using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;
using TukiFact.Application.DTOs.Documents;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Enums;

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
        _logger = logger;
    }

    public async Task<DocumentResponse> EmitAsync(CreateDocumentRequest request, Guid tenantId, CancellationToken ct = default)
    {
        // 1. Validate and get tenant
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Tenant no encontrado");

        // 2. Get series and next correlative
        var series = await _seriesRepo.GetByTypeAndSerieAsync(tenantId, request.DocumentType, request.Serie, ct)
            ?? throw new InvalidOperationException($"Serie {request.Serie} no encontrada para tipo {request.DocumentType}");

        if (!series.IsActive)
            throw new InvalidOperationException($"La serie '{request.Serie}' está inactiva.");

        var correlative = await _seriesRepo.GetNextCorrelativeAsync(series.Id, ct);

        // 3. Build document with calculated amounts
        var document = BuildDocument(request, tenantId, series, correlative);

        // 4. Save to DB
        await _documentRepo.CreateAsync(document, ct);
        _logger.LogInformation("Document created: {FullNumber}", document.FullNumber);

        // 5. Build UBL XML
        var xml = _ublBuilder.BuildInvoiceXml(document, tenant);

        // 6. Sign XML (if certificate available)
        string signedXml = xml;
        string? hashCode = null;
        if (tenant.CertificateData is not null && tenant.CertificatePasswordEncrypted is not null)
        {
            try
            {
                var (signed, digest) = _signingService.SignXml(xml, tenant.CertificateData, tenant.CertificatePasswordEncrypted);
                signedXml = signed;
                hashCode = digest;
                document.HashCode = hashCode;
                document.Status = DocumentStatus.Signed;
                _logger.LogInformation("Document signed: {FullNumber}, Hash: {Hash}", document.FullNumber, hashCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sign document {FullNumber}. Continuing without signature.", document.FullNumber);
            }
        }
        else
        {
            _logger.LogWarning("No certificate configured for tenant {TenantId}. Document will not be signed.", tenantId);
        }

        // 7. Store XML in MinIO
        var xmlBytes = Encoding.UTF8.GetBytes(signedXml);
        var xmlPath = await _storageService.UploadXmlAsync(tenantId,
            $"{document.FullNumber}.xml", xmlBytes, ct);
        document.XmlUrl = xmlPath;

        // 8. Send to SUNAT
        try
        {
            var zipBytes = CreateZipFromXml($"{tenant.Ruc}-{document.DocumentType}-{document.FullNumber}.xml", xmlBytes);
            var sunatResponse = await _sunatClient.SendDocumentAsync(
                tenant.Ruc, document.DocumentType, document.FullNumber, zipBytes, ct);

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

        return MapToResponse(document);
    }

    public async Task<DocumentResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _documentRepo.GetByIdWithItemsAsync(id, ct);
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
            IssueDate = request.IssueDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
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

        // Get the reference document
        var refDoc = await _documentRepo.GetByIdWithItemsAsync(request.ReferenceDocumentId, ct)
            ?? throw new InvalidOperationException("Documento de referencia no encontrado");

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

        await _documentRepo.CreateAsync(document, ct);
        _logger.LogInformation("CreditNote created: {FullNumber} for {RefNumber}", document.FullNumber, refDoc.FullNumber);

        return await ProcessAndSendDocument(document, tenant, ct);
    }

    public async Task<DocumentResponse> EmitDebitNoteAsync(CreateDebitNoteRequest request, Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct)
            ?? throw new InvalidOperationException("Tenant no encontrado");

        var refDoc = await _documentRepo.GetByIdWithItemsAsync(request.ReferenceDocumentId, ct)
            ?? throw new InvalidOperationException("Documento de referencia no encontrado");

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

        // Sign XML (if certificate available)
        string signedXml = xml;
        string? hashCode = null;
        if (tenant.CertificateData is not null && tenant.CertificatePasswordEncrypted is not null)
        {
            try
            {
                var (signed, digest) = _signingService.SignXml(xml, tenant.CertificateData, tenant.CertificatePasswordEncrypted);
                signedXml = signed;
                hashCode = digest;
                document.HashCode = hashCode;
                document.Status = DocumentStatus.Signed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sign document {FullNumber}.", document.FullNumber);
            }
        }

        // Store XML
        var xmlBytes = Encoding.UTF8.GetBytes(signedXml);
        var xmlPath = await _storageService.UploadXmlAsync(tenant.Id,
            $"{document.FullNumber}.xml", xmlBytes, ct);
        document.XmlUrl = xmlPath;

        // Send to SUNAT
        try
        {
            var zipBytes = CreateZipFromXml($"{tenant.Ruc}-{document.DocumentType}-{document.FullNumber}.xml", xmlBytes);
            var sunatResponse = await _sunatClient.SendDocumentAsync(
                tenant.Ruc, document.DocumentType, document.FullNumber, zipBytes, ct);

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

        document.QrData = $"{tenant.Ruc}|{document.DocumentType}|{document.Serie}|{document.Correlative}|{document.Igv:F2}|{document.Total:F2}|{document.IssueDate:yyyy-MM-dd}|{document.CustomerDocType}|{document.CustomerDocNumber}|{hashCode ?? ""}";

        await _documentRepo.UpdateAsync(document, ct);
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
            doc.Igv, doc.Total, doc.Status,
            doc.SunatResponseCode, doc.SunatResponseDescription,
            doc.HashCode, doc.XmlUrl, doc.PdfUrl, doc.Notes, doc.CreatedAt,
            doc.Items.OrderBy(i => i.Sequence).Select(i => new DocumentItemResponse(
                i.Sequence, i.ProductCode, i.Description, i.Quantity,
                i.UnitMeasure, i.UnitPrice, i.UnitPriceWithIgv,
                i.IgvType, i.IgvAmount, i.Subtotal, i.Total)).ToList());
    }
}
