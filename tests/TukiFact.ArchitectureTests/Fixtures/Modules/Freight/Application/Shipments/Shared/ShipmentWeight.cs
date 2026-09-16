namespace TukiFact.ArchitectureTests.Fixtures.Modules.Freight.Application.Shipments.Shared;

/// <summary>
/// Clean fixture: a <c>Shared</c> folder nested under a submodule (<c>Shipments</c>) is allowed —
/// only a top-level <c>Shared</c> folder directly under the layer root is banned
/// (structure convention).
/// </summary>
public sealed record ShipmentWeightInKilograms(decimal Value);
