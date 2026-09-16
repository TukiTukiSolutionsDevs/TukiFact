using TukiFact.ArchitectureTests.Fixtures.Modules.Freight.Domain;

namespace TukiFact.ArchitectureTests.Fixtures.Modules.Freight.Application;

public sealed record GetShipmentQuery(Guid ShipmentId);

/// <summary>
/// Clean fixture: internal sealed, next to its request (same namespace/file), depends on its own
/// Domain only — no Infrastructure/Presentation, no other module, no <c>ITransactionManager</c> or
/// repository port (application conventions).
/// </summary>
internal sealed class GetShipmentQueryHandler
{
    public Shipment Handle(GetShipmentQuery query) => new() { Id = query.ShipmentId, TrackingCode = "N/A" };
}
