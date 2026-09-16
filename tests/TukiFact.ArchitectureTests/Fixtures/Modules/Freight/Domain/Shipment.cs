namespace TukiFact.ArchitectureTests.Fixtures.Modules.Freight.Domain;

/// <summary>Clean fixture: Domain depends on the BCL only.</summary>
public sealed class Shipment
{
    public required Guid Id { get; init; }

    public required string TrackingCode { get; init; }
}
