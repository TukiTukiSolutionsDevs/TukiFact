using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Background worker that drives Comunicación de Baja (RA) end-to-end:
///   pending → SignAndSend → sent → PollStatus → accepted | rejected
/// Runs on a fixed interval; both pickup queries are batch-scoped.
/// </summary>
public class VoidedDocumentScheduler : BackgroundService
{
    private const int TickIntervalSeconds = 30;
    private const int MaxRetries = 5;
    private const int PollEverySeconds = 60;
    private const int BatchSize = 25;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VoidedDocumentScheduler> _logger;

    public VoidedDocumentScheduler(IServiceScopeFactory scopeFactory, ILogger<VoidedDocumentScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VoidedDocumentScheduler started (tick={Tick}s, maxRetries={Max}, pollEvery={Poll}s)",
            TickIntervalSeconds, MaxRetries, PollEverySeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
                await ProcessSentAsync(stoppingToken);
            }
            catch (OperationCanceledException) { /* shutdown */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VoidedDocumentScheduler tick failed");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(TickIntervalSeconds), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        _logger.LogInformation("VoidedDocumentScheduler stopped");
    }

    private async Task ProcessPendingAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IVoidedDocumentRepository>();
        var pending = await repo.GetPendingForWorkerAsync(MaxRetries, BatchSize, ct);
        if (pending.Count == 0) return;

        _logger.LogInformation("VoidedScheduler: processing {Count} pending tickets", pending.Count);

        foreach (var voided in pending)
        {
            if (ct.IsCancellationRequested) break;
            using var perItem = _scopeFactory.CreateScope();
            var service = perItem.ServiceProvider.GetRequiredService<IVoidedDocumentService>();
            // Re-fetch inside the per-item scope so we are working with that scope's DbContext-tracked entity.
            var perRepo = perItem.ServiceProvider.GetRequiredService<IVoidedDocumentRepository>();
            var fresh = await perRepo.GetByIdAsync(voided.Id, voided.TenantId, ct);
            if (fresh is null || fresh.Status != "pending") continue;
            await service.SignAndSendAsync(fresh, ct);
        }
    }

    private async Task ProcessSentAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IVoidedDocumentRepository>();
        var sent = await repo.GetSentForPollAsync(PollEverySeconds, BatchSize, ct);
        if (sent.Count == 0) return;

        _logger.LogInformation("VoidedScheduler: polling {Count} sent tickets", sent.Count);

        foreach (var voided in sent)
        {
            if (ct.IsCancellationRequested) break;
            using var perItem = _scopeFactory.CreateScope();
            var service = perItem.ServiceProvider.GetRequiredService<IVoidedDocumentService>();
            var perRepo = perItem.ServiceProvider.GetRequiredService<IVoidedDocumentRepository>();
            var fresh = await perRepo.GetByIdAsync(voided.Id, voided.TenantId, ct);
            if (fresh is null || fresh.Status != "sent") continue;
            await service.PollStatusAsync(fresh, ct);
        }
    }
}
