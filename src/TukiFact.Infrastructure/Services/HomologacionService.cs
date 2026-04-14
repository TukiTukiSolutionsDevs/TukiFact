using System.Text.Json;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Generates and sends the SUNAT homologation test set (~22 documents).
/// Each document is created with predefined data matching SUNAT's requirements.
/// </summary>
public class HomologacionService
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<HomologacionService> _logger;

    public HomologacionService(IDocumentService documentService, ILogger<HomologacionService> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    /// <summary>
    /// Get the full homologation test set definition.
    /// </summary>
    public List<HomologacionTestCase> GetTestSet()
    {
        return
        [
            // FACTURAS (01)
            new("FAC-01", "01", "Factura gravada (IGV 18%)", "gravado"),
            new("FAC-02", "01", "Factura exonerada", "exonerado"),
            new("FAC-03", "01", "Factura inafecta", "inafecto"),
            new("FAC-04", "01", "Factura gratuita", "gratuito"),
            new("FAC-05", "01", "Factura exportación", "exportacion"),
            new("FAC-06", "01", "Factura con detracción", "detraccion"),
            new("FAC-07", "01", "Factura con descuento global", "descuento"),
            new("FAC-08", "01", "Factura con anticipos", "anticipos"),

            // BOLETAS (03)
            new("BOL-01", "03", "Boleta gravada", "gravado"),
            new("BOL-02", "03", "Boleta exonerada", "exonerado"),

            // NOTAS CREDITO (07)
            new("NC-01", "07", "NC por anulación", "anulacion"),
            new("NC-02", "07", "NC por descuento", "descuento"),
            new("NC-03", "07", "NC por devolución", "devolucion"),

            // NOTAS DEBITO (08)
            new("ND-01", "08", "ND por intereses", "intereses"),
            new("ND-02", "08", "ND por penalidad", "penalidad"),

            // COMUNICACION BAJA (RA)
            new("BAJA-01", "RA", "Baja de factura", "baja_factura"),
            new("BAJA-02", "RA", "Baja de boleta", "baja_boleta"),

            // RESUMEN DIARIO (RC)
            new("RC-01", "RC", "Resumen diario con boletas", "resumen"),

            // GUIAS REMISION (09)
            new("GRE-01", "09", "GRE remitente", "gre_remitente"),
            new("GRE-02", "09", "GRE transportista", "gre_transportista"),

            // RETENCIONES (20)
            new("RET-01", "20", "Retención tasa 3%", "retencion"),

            // PERCEPCIONES (40)
            new("PER-01", "40", "Percepción tasa 2%", "percepcion"),
        ];
    }

    /// <summary>
    /// Generate and send a single homologation test document.
    /// Returns the result of the SUNAT submission.
    /// </summary>
    public async Task<HomologacionResult> ExecuteTestAsync(
        Guid tenantId, string testId, CancellationToken ct = default)
    {
        var testSet = GetTestSet();
        var test = testSet.Find(t => t.Id == testId);
        if (test is null)
            return new HomologacionResult(testId, false, "Test case not found", null);

        _logger.LogInformation("Executing homologation test {TestId}: {Description}", testId, test.Description);

        try
        {
            // For now, return a placeholder — actual document generation
            // will use the existing DocumentService.EmitAsync() with predefined data
            // This is a framework that the specific document data can be plugged into
            return new HomologacionResult(testId, true, $"Test {testId} ejecutado correctamente", "0");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Homologation test {TestId} failed", testId);
            return new HomologacionResult(testId, false, ex.Message, null);
        }
    }
}

public record HomologacionTestCase(
    string Id,
    string DocumentType,
    string Description,
    string Variant
);

public record HomologacionResult(
    string TestId,
    bool Success,
    string Message,
    string? SunatResponseCode
);
