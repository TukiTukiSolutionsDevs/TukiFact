namespace TukiFact.ArchitectureTests.Fixtures.Modules.Freight.Domain;

/// <summary>Clean fixture: 3 public methods (≤5), no <see cref="IQueryable{T}"/> anywhere in its shape.</summary>
public interface IShipmentRepository
{
    void Add(Shipment shipment);

    void Remove(Shipment shipment);

    Shipment? Find(Guid id);
}
