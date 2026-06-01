using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Services;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Background service that materializes due recurring invoices into real SUNAT submissions.
/// Runs hourly. For each due schedule:
///   1. Tries to claim a per-row lease (so two workers during deploy overlap don't double-emit).
///   2. Atomically reserves an emission slot (UNIQUE idempotency on schedule+target date)
///      AND advances NextEmissionDate in the same transaction — a crash after this point
///      means the next tick will see the slot already reserved and skip, never duplicating.
///   3. Calls IDocumentService.EmitAsync (same code path as the manual /v1/documents POST).
///   4. Finalizes the emission row with result + bumps counters or auto-pauses after N failures.
///
/// DIFERENCIADOR: Nubefact NO tiene facturación recurrente.
/// </summary>
public class RecurringInvoiceScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecurringInvoiceScheduler> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(10);
    private const int AutoPauseAfterFailures = 3;

    public RecurringInvoiceScheduler(IServiceProvider serviceProvider, ILogger<RecurringInvoiceScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecurringInvoiceScheduler started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueInvoicesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RecurringInvoiceScheduler");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("RecurringInvoiceScheduler stopped");
    }

    private async Task ProcessDueInvoicesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var recurringRepo = scope.ServiceProvider.GetRequiredService<IRecurringInvoiceRepository>();
        var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

        var today = RecurringScheduleCalculator.TodayInLima();
        var dueInvoices = await recurringRepo.GetDueForEmissionAsync(today, ct);

        if (dueInvoices.Count == 0)
        {
            _logger.LogDebug("No recurring invoices due for emission today ({Today})", today);
            return;
        }

        _logger.LogInformation("Found {Count} recurring invoices due for emission", dueInvoices.Count);

        foreach (var recurring in dueInvoices)
        {
            try
            {
                await EmitFromRecurringAsync(recurring, documentService, recurringRepo, today, ct);
            }
            catch (Exception ex)
            {
                // Per-row exception isolation — one tenant's schedule blowing up must not stop the rest.
                _logger.LogError(ex, "Unhandled exception emitting recurring invoice {Id} for tenant {TenantId}",
                    recurring.Id, recurring.TenantId);
            }
        }
    }

    private async Task EmitFromRecurringAsync(
        RecurringInvoice recurring,
        IDocumentService documentService,
        IRecurringInvoiceRepository recurringRepo,
        DateOnly today,
        CancellationToken ct)
    {
        var targetDate = recurring.NextEmissionDate ?? today;

        // 1. Claim the lease so no other worker picks this row while we work on it.
        var lockUntil = DateTimeOffset.UtcNow.Add(LeaseDuration);
        if (!await recurringRepo.TryClaimAsync(recurring.Id, lockUntil, ct))
        {
            _logger.LogDebug("Recurring invoice {Id} is locked by another worker — skipping", recurring.Id);
            return;
        }

        try
        {
            // 2. Advance NextEmissionDate + reserve emission slot atomically.
            //    Doing this BEFORE EmitAsync means a crash mid-emit doesn't re-emit next tick.
            var nextAfter = ComputeNextAfterTarget(recurring, targetDate);

            var emission = await recurringRepo.ReserveEmissionAsync(recurring, targetDate, nextAfter, ct);
            if (emission is null)
            {
                // Idempotent collision — another path (likely a crashed previous run) already
                // reserved this exact (schedule, date) pair. Don't emit again.
                _logger.LogWarning(
                    "Skipping duplicate emission for recurring {Id} target {Target} — slot already reserved.",
                    recurring.Id, targetDate);
                return;
            }

            _logger.LogInformation(
                "Emitting recurring invoice {Id} (target {Target}) — Serie {Serie} for tenant {TenantId}",
                recurring.Id, targetDate, recurring.Serie, recurring.TenantId);

            // 3. Materialize through the same pipeline as a manual emission.
            var items = System.Text.Json.JsonSerializer
                .Deserialize<List<Application.DTOs.Documents.CreateDocumentItemRequest>>(recurring.ItemsJson) ?? [];

            var request = new Application.DTOs.Documents.CreateDocumentRequest(
                recurring.DocumentType,
                recurring.Serie,
                targetDate,
                null,
                recurring.Currency,
                recurring.CustomerDocType,
                recurring.CustomerDocNumber,
                recurring.CustomerName,
                recurring.CustomerAddress,
                recurring.CustomerEmail,
                $"Factura recurrente automática #{recurring.EmittedCount + 1}",
                null,
                items
            );

            try
            {
                var result = await documentService.EmitAsync(request, recurring.TenantId, ct);
                emission.DocumentId = result.Id;
                emission.SunatResponseCode = result.SunatResponseCode;

                if (string.Equals(result.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
                {
                    emission.Status = "succeeded";
                    OnSuccess(recurring);
                    _logger.LogInformation(
                        "Recurring {Id} emission {Target} accepted by SUNAT — {FullNumber}",
                        recurring.Id, targetDate, result.FullNumber);
                }
                else if (string.Equals(result.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
                {
                    emission.Status = "rejected";
                    emission.ErrorMessage = Truncate(result.SunatResponseDescription, 2000)
                        ?? "SUNAT rechazó el comprobante sin descripción";
                    OnFailure(recurring, emission.ErrorMessage);
                    _logger.LogWarning(
                        "Recurring {Id} emission {Target} REJECTED by SUNAT: {Desc}",
                        recurring.Id, targetDate, result.SunatResponseDescription);
                }
                else
                {
                    // Status == Sent / Signed / etc — SUNAT didn't confirm. Treat as pending failure
                    // so we can retry next cycle; do NOT count as success.
                    emission.Status = "rejected";
                    emission.ErrorMessage = $"SUNAT no confirmó la emisión (estado {result.Status}).";
                    OnFailure(recurring, emission.ErrorMessage);
                    _logger.LogWarning(
                        "Recurring {Id} emission {Target} not confirmed by SUNAT — status {Status}",
                        recurring.Id, targetDate, result.Status);
                }
            }
            catch (Exception ex)
            {
                emission.Status = "error";
                emission.ErrorMessage = Truncate(ex.Message, 2000);
                OnFailure(recurring, emission.ErrorMessage ?? "Error inesperado");
                _logger.LogError(ex,
                    "Recurring {Id} emission {Target} threw an exception",
                    recurring.Id, targetDate);
            }

            // 4. Check if EndDate reached (after a successful or failed cycle the schedule may have closed).
            if (recurring.EndDate.HasValue && recurring.NextEmissionDate.HasValue
                && recurring.NextEmissionDate.Value > recurring.EndDate.Value)
            {
                recurring.Status = "completed";
                recurring.NextEmissionDate = null;
                _logger.LogInformation(
                    "Recurring {Id} reached EndDate — marking completed (total emissions: {Count})",
                    recurring.Id, recurring.EmittedCount);
            }

            await recurringRepo.FinalizeEmissionAsync(emission, recurring, ct);
        }
        finally
        {
            // Always release the lease — a held lease blocks future ticks for LeaseDuration.
            await recurringRepo.ReleaseClaimAsync(recurring.Id, ct);
        }
    }

    private static void OnSuccess(RecurringInvoice recurring)
    {
        recurring.EmittedCount++;
        recurring.ConsecutiveFailures = 0;
        recurring.LastError = null;
    }

    private static void OnFailure(RecurringInvoice recurring, string error)
    {
        recurring.ConsecutiveFailures++;
        recurring.LastError = Truncate(error, 2000);

        if (recurring.ConsecutiveFailures >= AutoPauseAfterFailures)
        {
            recurring.Status = "paused";
            recurring.NextEmissionDate = null;
        }
    }

    private static DateOnly ComputeNextAfterTarget(RecurringInvoice recurring, DateOnly target)
    {
        return RecurringScheduleCalculator.AdvanceFrom(
            target, recurring.Frequency, recurring.DayOfMonth, recurring.DayOfWeek, recurring.StartDate);
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= max ? value : value[..max];
    }
}
