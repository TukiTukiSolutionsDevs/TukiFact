using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using TukiFact.ArchitectureTests.Fixtures.Modules.Freight.Application;
using TukiFact.Common.Presentation.Endpoints;

namespace TukiFact.ArchitectureTests.Fixtures.Modules.Freight.Presentation.Shipments;

/// <summary>
/// Clean fixture: a real <see cref="IEndpoint"/>, maps to the Application layer only, sealed, named
/// <c>&lt;UseCase&gt;Endpoint</c>, and — unlike Ledger's endpoints — placed under a submodule folder
/// (<c>Shipments</c>), not directly at the Presentation root.
/// </summary>
public sealed class GetShipmentEndpoint : IEndpoint
{
    public object Handle(GetShipmentQuery query) => new GetShipmentQueryHandler().Handle(query);

    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/shipments/{id}", () => Handle(new GetShipmentQuery(Guid.Empty)));
}
