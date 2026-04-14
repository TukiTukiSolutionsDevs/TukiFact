using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Background service that checks for recurring invoices due for emission.
/// Runs every hour, checks NextEmissionDate <= today, emits document, updates schedule.
/// DIFERENCIADOR: Nubefact NO tiene facturación recurrente.
/// </summary>
public class RecurringInvoiceScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecurringInvoiceScheduler> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
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
                await EmitFromRecurringAsync(recurring, documentService, recurringRepo, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to emit recurring invoice {Id} for tenant {TenantId}",
                    recurring.Id, recurring.TenantId);
            }
        }
    }

    private async Task EmitFromRecurringAsync(
        RecurringInvoice recurring,
        IDocumentService documentService,
        IRecurringInvoiceRepository recurringRepo,
        CancellationToken ct)
    {
        _logger.LogInformation("Emitting recurring invoice {Id} — Serie {Serie} for tenant {TenantId}",
            recurring.Id, recurring.Serie, recurring.TenantId);

        // Deserialize items from JSON template
        var items = System.Text.Json.JsonSerializer.Deserialize<List<Application.DTOs.Documents.CreateDocumentItemRequest>>(
            recurring.ItemsJson) ?? [];

        // Create document request from recurring template
        var request = new Application.DTOs.Documents.CreateDocumentRequest(
            recurring.DocumentType,
            recurring.Serie,
            DateOnly.FromDateTime(DateTime.UtcNow),
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

        // Emit
        var result = await documentService.EmitAsync(request, recurring.TenantId, ct);
        _logger.LogInformation("Recurring invoice emitted: {FullNumber} — Status: {Status}",
            result.FullNumber, result.Status);

        // Update recurring invoice
        recurring.EmittedCount++;
        recurring.LastEmittedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        recurring.NextEmissionDate = CalculateNextEmissionDate(recurring);

        // Check if completed
        if (recurring.EndDate.HasValue && recurring.NextEmissionDate > recurring.EndDate)
        {
            recurring.Status = "completed";
            recurring.NextEmissionDate = null;
            _logger.LogInformation("Recurring invoice {Id} completed — total emissions: {Count}",
                recurring.Id, recurring.EmittedCount);
        }

        await recurringRepo.UpdateAsync(recurring, ct);
    }

    private static DateOnly? CalculateNextEmissionDate(RecurringInvoice recurring)
    {
        var current = recurring.NextEmissionDate ?? recurring.StartDate;

        return recurring.Frequency switch
        {
            "daily" => current.AddDays(1),
            "weekly" => current.AddDays(7),
            "biweekly" => current.AddDays(14),
            "monthly" => recurring.DayOfMonth.HasValue
                ? GetNextMonthDate(current, recurring.DayOfMonth.Value)
                : current.AddMonths(1),
            "yearly" => current.AddYears(1),
            _ => current.AddMonths(1)
        };
    }

    private static DateOnly GetNextMonthDate(DateOnly current, int dayOfMonth)
    {
        var nextMonth = current.AddMonths(1);
        var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
        var day = Math.Min(dayOfMonth, daysInMonth);
        return new DateOnly(nextMonth.Year, nextMonth.Month, day);
    }
}
