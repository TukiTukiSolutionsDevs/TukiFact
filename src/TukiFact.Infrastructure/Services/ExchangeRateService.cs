using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Fetches and caches exchange rates from decolecta.com (SBS/SUNAT source).
/// Cache: 1 query per day per currency, stored in exchange_rates table.
/// </summary>
public class ExchangeRateService : IExchangeRateService
{
    private readonly ILogger<ExchangeRateService> _logger;
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _db;
    private const string BaseUrl = "https://api.decolecta.com/v1/tipo-cambio/sunat";

    public ExchangeRateService(
        ILogger<ExchangeRateService> logger,
        IHttpClientFactory httpClientFactory,
        AppDbContext db)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Decolecta");
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
            var url = $"{BaseUrl}?date={date:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct);
            var json = JsonDocument.Parse(body);
            var root = json.RootElement;

            var buyRate = root.TryGetProperty("buy_price", out var buy)
                ? decimal.Parse(buy.GetString() ?? "0", System.Globalization.CultureInfo.InvariantCulture)
                : 0m;
            var sellRate = root.TryGetProperty("sell_price", out var sell)
                ? decimal.Parse(sell.GetString() ?? "0", System.Globalization.CultureInfo.InvariantCulture)
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
