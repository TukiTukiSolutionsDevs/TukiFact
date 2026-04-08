namespace TukiFact.Domain.Entities;

public class Plan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceMonthly { get; set; }
    public int MaxDocumentsPerMonth { get; set; }
    public string Features { get; set; } = "{}"; // JSONB stored as string
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
}
