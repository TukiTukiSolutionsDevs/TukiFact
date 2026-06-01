using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Infrastructure.Services.EventHandlers;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Worker-side lifecycle for Comunicación de Baja (RA).
/// Owns: build XML → sign with tenant cert → zip → SendSummary to SUNAT → persist ticket+status.
/// Plus: poll getStatus until SUNAT returns a final code.
/// </summary>
public class VoidedDocumentService : IVoidedDocumentService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly IVoidedDocumentRepository _voidedRepo;
    private readonly ITenantRepository _tenantRepo;
    private readonly IVoidedDocumentXmlBuilder _xmlBuilder;
    private readonly IXmlSigningService _signingService;
    private readonly ISunatClient _sunatClient;
    private readonly IStorageService _storage;
    private readonly ISecretProtector _secrets;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<VoidedDocumentService> _logger;

    public VoidedDocumentService(
        IVoidedDocumentRepository voidedRepo,
        ITenantRepository tenantRepo,
        IVoidedDocumentXmlBuilder xmlBuilder,
        IXmlSigningService signingService,
        ISunatClient sunatClient,
        IStorageService storage,
        ISecretProtector secrets,
        IEventPublisher eventPublisher,
        ILogger<VoidedDocumentService> logger)
    {
        _voidedRepo = voidedRepo;
        _tenantRepo = tenantRepo;
        _xmlBuilder = xmlBuilder;
        _signingService = signingService;
        _sunatClient = sunatClient;
        _storage = storage;
        _secrets = secrets;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    private async Task PublishVoidedAcceptedAsync(VoidedDocument voided, Tenant tenant, CancellationToken ct)
    {
        // ItemsJson typically has a single entry (one void per request). Parse the first to surface
        // the voided doc number in the event so handlers can render "Anulado F001-00000001".
        string? fullNumber = null;
        string? documentType = null;
        try
        {
            var items = JsonSerializer.Deserialize<List<JsonElement>>(voided.ItemsJson, JsonOpts);
            if (items is { Count: > 0 })
            {
                if (items[0].TryGetProperty("fullNumber", out var fn)) fullNumber = fn.GetString();
                if (items[0].TryGetProperty("documentType", out var dt)) documentType = dt.GetString();
            }
        }
        catch { /* event metadata is best-effort */ }

        try
        {
            await _eventPublisher.PublishAsync("document.voided", new TukiFactEvent
            {
                TenantId = tenant.Id,
                EntityId = voided.Id,
                EntityType = "VoidedDocument",
                EventType = "document.voided",
                DocumentType = documentType,
                FullNumber = fullNumber ?? voided.TicketNumber,
                Status = voided.Status,
                SunatResponseCode = voided.SunatResponseCode,
                SunatResponseDescription = voided.SunatResponseDescription
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Publish document.voided failed for ticket {Ticket}", voided.TicketNumber);
        }
    }

    public async Task SignAndSendAsync(VoidedDocument voided, CancellationToken ct = default)
    {
        try
        {
            var tenant = await _tenantRepo.GetByIdAsync(voided.TenantId, ct)
                ?? throw new InvalidOperationException($"Tenant {voided.TenantId} not found");

            // Hard prereqs — same gates DocumentService uses
            if (tenant.CertificateData is null || string.IsNullOrEmpty(tenant.CertificatePasswordEncrypted))
                throw new InvalidOperationException("Tenant sin certificado digital configurado");
            if (string.IsNullOrEmpty(tenant.SunatUser) || string.IsNullOrEmpty(tenant.SunatPasswordEncrypted))
                throw new InvalidOperationException("Tenant sin credenciales SOL configuradas");

            voided.Status = "signing";
            await _voidedRepo.UpdateAsync(voided, ct);

            var xml = _xmlBuilder.BuildVoidedXml(voided, tenant);
            var (signedXml, _) = _signingService.SignXml(
                xml, tenant.CertificateData, _secrets.Unprotect(tenant.CertificatePasswordEncrypted));

            var xmlBytes = Encoding.UTF8.GetBytes(signedXml);
            var xmlFileName = $"{tenant.Ruc}-{voided.TicketNumber}.xml";
            voided.XmlUrl = await _storage.UploadXmlAsync(tenant.Id, xmlFileName, xmlBytes, ct);

            var zipBytes = CreateZipFromXml(xmlFileName, xmlBytes);

            var creds = new SunatCredentials(
                tenant.SunatUser!,
                _secrets.Unprotect(tenant.SunatPasswordEncrypted!),
                tenant.Environment);

            var response = await _sunatClient.SendSummaryAsync(
                tenant.Ruc, voided.TicketNumber, zipBytes, creds, ct);

            if (!response.Success)
            {
                voided.Status = "pending"; // re-queue for retry
                voided.RetryCount++;
                voided.LastError = $"SendSummary fail: {response.ErrorMessage ?? response.Description}";
                await _voidedRepo.UpdateAsync(voided, ct);
                _logger.LogWarning("Voided {Ticket} send failed: {Err}", voided.TicketNumber, voided.LastError);
                return;
            }

            // Beta stub returns Success=true ResponseCode="0" with no real ticket → treat as accepted immediately.
            // Production returns a real SUNAT ticket number in ResponseCode → must poll getStatus to confirm.
            if (string.Equals(tenant.Environment, "beta", StringComparison.OrdinalIgnoreCase))
            {
                voided.Status = "accepted";
                voided.SunatResponseCode = response.ResponseCode;
                voided.SunatResponseDescription = response.Description ?? "Aceptado (beta stub)";
                voided.LastError = null;
            }
            else
            {
                voided.SunatTicket = response.ResponseCode;
                voided.Status = "sent";
                voided.SunatResponseDescription = response.Description;
                voided.LastError = null;
            }

            await _voidedRepo.UpdateAsync(voided, ct);
            _logger.LogInformation("Voided {Ticket} → {Status} (sunat_ticket={SunatTicket})",
                voided.TicketNumber, voided.Status, voided.SunatTicket ?? "n/a");

            if (voided.Status == "accepted")
                await PublishVoidedAcceptedAsync(voided, tenant, ct);
        }
        catch (Exception ex)
        {
            voided.Status = "pending"; // worker will retry while under maxRetries
            voided.RetryCount++;
            voided.LastError = ex.Message;
            await _voidedRepo.UpdateAsync(voided, ct);
            _logger.LogError(ex, "SignAndSend failed for voided {Ticket}", voided.TicketNumber);
        }
    }

    public async Task PollStatusAsync(VoidedDocument voided, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(voided.SunatTicket))
            return; // nothing to poll

        try
        {
            var tenant = await _tenantRepo.GetByIdAsync(voided.TenantId, ct)
                ?? throw new InvalidOperationException($"Tenant {voided.TenantId} not found");

            var creds = new SunatCredentials(
                tenant.SunatUser ?? string.Empty,
                string.IsNullOrEmpty(tenant.SunatPasswordEncrypted) ? string.Empty : _secrets.Unprotect(tenant.SunatPasswordEncrypted),
                tenant.Environment);

            var response = await _sunatClient.GetStatusAsync(voided.SunatTicket, creds, ct);

            voided.LastPolledAt = DateTimeOffset.UtcNow;
            voided.SunatResponseCode = response.ResponseCode;
            voided.SunatResponseDescription = response.Description;

            // SUNAT codes: 0=ok with CDR, 98=still processing, 99=processing with errors, others=rejected
            if (response.Success && response.ResponseCode == "0")
            {
                voided.Status = "accepted";
                if (response.CdrZip is not null)
                {
                    var cdrPath = await _storage.UploadCdrAsync(
                        tenant.Id, $"R-{voided.TicketNumber}.zip", response.CdrZip, ct);
                    voided.CdrUrl = cdrPath;
                }
                voided.LastError = null;
            }
            else if (response.ResponseCode is "98" or "99")
            {
                // still processing — keep as sent, will repoll
            }
            else
            {
                voided.Status = "rejected";
                voided.LastError = response.ErrorMessage ?? response.Description;
            }

            await _voidedRepo.UpdateAsync(voided, ct);
            _logger.LogInformation("Voided {Ticket} poll → {Status} (sunat={Code})",
                voided.TicketNumber, voided.Status, voided.SunatResponseCode);

            if (voided.Status == "accepted")
                await PublishVoidedAcceptedAsync(voided, tenant, ct);
        }
        catch (Exception ex)
        {
            voided.LastPolledAt = DateTimeOffset.UtcNow;
            voided.LastError = ex.Message;
            await _voidedRepo.UpdateAsync(voided, ct);
            _logger.LogError(ex, "PollStatus failed for voided {Ticket}", voided.TicketNumber);
        }
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
}
