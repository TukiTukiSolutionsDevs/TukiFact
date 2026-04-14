namespace TukiFact.Domain.Entities;

public class ExchangeRate
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal BuyRate { get; set; }
    public decimal SellRate { get; set; }
    public string Source { get; set; } = "SBS";
    public DateTimeOffset FetchedAt { get; set; } = DateTimeOffset.UtcNow;
}
