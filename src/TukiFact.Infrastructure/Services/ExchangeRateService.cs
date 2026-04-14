using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Fetches and caches exchange rates from apis.net.pe (SBS/SUNAT source).
/// Cache: 1 query per day per currency, stored in exchange_rates table.
/// </summary>
public class ExchangeRateService : IExchangeRateService
{
    private readonly ILogger<ExchangeRateService> _logger;
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _db;
    private const string BaseUrl = "https://api.apis.net.pe/v2/sunat/tipo-cambio";

    public ExchangeRateService(
        ILogger<ExchangeRateService> logger,
        IHttpClientFactory httpClientFactory,
        AppDbContext db)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("ApisNetPe");
        _db = db;
    }

    public async Task<ExchangeRate?> GetRateAsync(DateOnly date, string currency = "USD", CancellationToken ct = default)
    {
        // Check cache first
        var cached = await _db.ExchangeRates
            .FirstOrDefaultAsync(r => r.Date == date && r.Currency == currency, ct);

        if (cached is not null)
            return cached;

        // Fetch from API
        return await FetchAndSaveRateAsync(date, currency, ct);
    }

    public async Task<ExchangeRate> FetchAndSaveRateAsync(DateOnly date, string currency = "USD", CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching exchange rate for {Date} {Currency}", date, currency);

        try
        {
            var url = $"{BaseUrl}?fecha={date:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(body);
            var root = json.RootElement;

            var buyRate = root.TryGetProperty("compra", out var compra)
                ? decimal.Parse(compra.GetString() ?? "0")
                : 0m;
            var sellRate = root.TryGetProperty("venta", out var venta)
                ? decimal.Parse(venta.GetString() ?? "0")
                : 0m;

            // Upsert
            var existing = await _db.ExchangeRates
                .FirstOrDefaultAsync(r => r.Date == date && r.Currency == currency, ct);

            if (existing is not null)
            {
                existing.BuyRate = buyRate;
                existing.SellRate = sellRate;
                existing.FetchedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                existing = new ExchangeRate
                {
                    Date = date,
                    Currency = currency,
                    BuyRate = buyRate,
                    SellRate = sellRate,
                    Source = "SBS",
                    FetchedAt = DateTimeOffset.UtcNow
                };
                _db.ExchangeRates.Add(existing);
            }

            await _db.SaveChangesAsync(ct);
            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching exchange rate for {Date}", date);
            throw;
        }
    }
}
