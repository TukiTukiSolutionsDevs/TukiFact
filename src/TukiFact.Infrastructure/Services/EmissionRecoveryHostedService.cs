using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TukiFact.Domain.Enums;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Recovery scan for C7 atomic materialization (Sprint: antes de producción, blocker #2).
///
/// Detects documents / perceptions / retentions that are stuck in <c>signed</c> state
/// (HashCode + XmlUrl persisted before SUNAT call) without a SUNAT response code, meaning
/// the SOAP call either died mid-flight or the response was lost before final UpdateAsync.
///
/// Currently flags + logs only (does NOT auto-resend). Reason: <c>sendBill</c> is not
/// strictly idempotent in our wrapper, and an auto-resend could double-emit if SUNAT
/// already received and we just lost the response. Operator must reconcile manually
/// (XML is in MinIO, hash is in DB, SUNAT inbox can be checked).
///
/// Future: implement <c>getStatusCDR</c> SOAP call on SunatClient to query SUNAT for the
/// CDR of an already-received doc; then this worker can auto-reconcile.
/// </summary>
public class EmissionRecoveryHostedService : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan StuckThreshold = TimeSpan.FromSeconds(120);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmissionRecoveryHostedService> _logger;

    public EmissionRecoveryHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmissionRecoveryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "EmissionRecoveryHostedService started (scan every {Interval}, threshold {Threshold})",
            ScanInterval, StuckThreshold);

        // Run once at startup, then on interval.
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ScanOnceAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "EmissionRecovery scan failed"); }

            try { await Task.Delay(ScanInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        _logger.LogInformation("EmissionRecoveryHostedService stopped");
    }

    private async Task ScanOnceAsync(CancellationToken ct)
    {
        var threshold = DateTimeOffset.UtcNow - StuckThreshold;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Documents (factura/boleta/NC/ND): stuck if signed + no SUNAT response + not freshly updated.
        var stuckDocs = await db.Documents
            .Where(d => d.Status == DocumentStatus.Signed
                        && d.SunatResponseCode == null
                        && d.UpdatedAt < threshold)
            .Select(d => new { d.Id, d.TenantId, d.FullNumber, d.UpdatedAt })
            .Take(50)
            .ToListAsync(ct);

        foreach (var d in stuckDocs)
        {
            _logger.LogError(
                "RECOVERY: document {FullNumber} (id={Id}, tenant={TenantId}) stuck in 'signed' since {UpdatedAt}. " +
                "Correlative consumed, XML signed and in MinIO, SUNAT response missing. Manual reconciliation required.",
                d.FullNumber, d.Id, d.TenantId, d.UpdatedAt);
        }

        // Perceptions (type 40)
        var stuckPerc = await db.PerceptionDocuments
            .Where(p => p.Status == DocumentStatus.Signed
                        && p.SunatResponseCode == null
                        && p.UpdatedAt < threshold)
            .Select(p => new { p.Id, p.TenantId, p.FullNumber, p.UpdatedAt })
            .Take(50)
            .ToListAsync(ct);

        foreach (var p in stuckPerc)
        {
            _logger.LogError(
                "RECOVERY: perception {FullNumber} (id={Id}, tenant={TenantId}) stuck in 'signed' since {UpdatedAt}. " +
                "Manual reconciliation required.",
                p.FullNumber, p.Id, p.TenantId, p.UpdatedAt);
        }

        // Retentions (type 20)
        var stuckRet = await db.RetentionDocuments
            .Where(r => r.Status == DocumentStatus.Signed
                        && r.SunatResponseCode == null
                        && r.UpdatedAt < threshold)
            .Select(r => new { r.Id, r.TenantId, r.FullNumber, r.UpdatedAt })
            .Take(50)
            .ToListAsync(ct);

        foreach (var r in stuckRet)
        {
            _logger.LogError(
                "RECOVERY: retention {FullNumber} (id={Id}, tenant={TenantId}) stuck in 'signed' since {UpdatedAt}. " +
                "Manual reconciliation required.",
                r.FullNumber, r.Id, r.TenantId, r.UpdatedAt);
        }

        if (stuckDocs.Count + stuckPerc.Count + stuckRet.Count > 0)
        {
            _logger.LogWarning(
                "EmissionRecovery: {Docs} document(s), {Perc} perception(s), {Ret} retention(s) stuck pending reconciliation",
                stuckDocs.Count, stuckPerc.Count, stuckRet.Count);
        }
    }
}
