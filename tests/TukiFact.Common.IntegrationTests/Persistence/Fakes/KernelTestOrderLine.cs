using TukiFact.Common.Domain.Entities;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

/// <summary>Child entity of <see cref="KernelTestOrder"/>; proves the fake module is more than a single flat table.</summary>
public sealed class KernelTestOrderLine : Entity<Guid>
{
    private KernelTestOrderLine(Guid id, Guid orderId, string description, int quantity)
    {
        Id = id;
        OrderId = orderId;
        Description = description;
        Quantity = quantity;
    }

    public Guid OrderId { get; private set; }

    public string Description { get; private set; }

    public int Quantity { get; private set; }

    internal static KernelTestOrderLine Create(Guid id, Guid orderId, string description, int quantity) =>
        new(id, orderId, description, quantity);
}
