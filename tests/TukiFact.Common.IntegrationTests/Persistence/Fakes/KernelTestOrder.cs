using TukiFact.Common.Domain.Entities;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

/// <summary>Test-only tenant-scoped aggregate that exercises the kernel persistence seam once, end to end.</summary>
public sealed class KernelTestOrder : AggregateRoot<Guid>
{
    private readonly List<KernelTestOrderLine> _lines = [];

    private KernelTestOrder(Guid id, Guid tenantId, string reference)
    {
        Id = id;
        TenantId = tenantId;
        Reference = reference;
    }

    public Guid TenantId { get; private set; }

    public string Reference { get; private set; }

    public IReadOnlyCollection<KernelTestOrderLine> Lines => _lines;

    public static KernelTestOrder Register(Guid id, Guid tenantId, string reference, DateTimeOffset occurredOn)
    {
        var order = new KernelTestOrder(id, tenantId, reference);
        order.RaiseDomainEvent(new KernelTestOrderRegisteredDomainEvent(id, tenantId, reference, occurredOn));
        return order;
    }

    public void Relabel(string reference, DateTimeOffset occurredOn)
    {
        Reference = reference;
        RaiseDomainEvent(new KernelTestOrderRelabeledDomainEvent(Id, reference, occurredOn));
    }

    public void AddLine(string description, int quantity) =>
        _lines.Add(KernelTestOrderLine.Create(Guid.NewGuid(), Id, description, quantity));
}
