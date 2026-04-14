namespace TukiFact.Domain.Entities;

/// <summary>
/// Tracks tenant subscription lifecycle: plan, billing dates, usage.
/// Payment gateway integration (Stripe/MercadoPago) is POST-DEPLOY.
/// For now: managed manually from backoffice.
/// </summary>
public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public string Status { get; set; } = "active"; // active, past_due, cancelled, trial
    public DateTimeOffset StartDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndDate { get; set; }
    public DateTimeOffset NextBillingDate { get; set; }
    public decimal MonthlyAmount { get; set; }
    public int DocumentsUsedThisMonth { get; set; }
    public int DocumentsLimit { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
}
