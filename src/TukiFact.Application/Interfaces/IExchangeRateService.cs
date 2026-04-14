using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IExchangeRateService
{
    Task<ExchangeRate?> GetRateAsync(DateOnly date, string currency = "USD", CancellationToken ct = default);
    Task<ExchangeRate> FetchAndSaveRateAsync(DateOnly date, string currency = "USD", CancellationToken ct = default);
}
