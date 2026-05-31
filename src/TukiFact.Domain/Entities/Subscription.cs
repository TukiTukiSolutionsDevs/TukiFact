namespace TukiFact.Domain.Entities;

/// <summary>
/// Tracks tenant subscription lifecycle + Culqi payment gateway wiring.
/// On subscribe: we create a Culqi customer, attach a card from a frontend tokenization,
/// then create a recurring subscription on Culqi. Culqi charges monthly and posts to our webhook.
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

    // Culqi gateway IDs (null until first subscribe; cleared on cancel only if
    // we delete the Culqi sub — customer/card stay to allow re-subscribe).
    public string? CulqiCustomerId { get; set; }
    public string? CulqiCardId { get; set; }
    public string? CulqiSubscriptionId { get; set; }
    public string? LastChargeId { get; set; }
    public DateTimeOffset? LastChargedAt { get; set; }
    public string? CancellationReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Plan Plan { get; set; } = null!;
}
