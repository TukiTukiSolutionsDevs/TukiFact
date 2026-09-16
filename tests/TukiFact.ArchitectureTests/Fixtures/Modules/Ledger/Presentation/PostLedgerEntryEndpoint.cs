using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Application;
using TukiFact.Common.Presentation.Endpoints;

namespace TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Presentation;

/// <summary>
/// Clean fixture in the violating module (maps to the Application layer only), but still a
/// deliberate composition violation: a real <see cref="IEndpoint"/> mapped directly at the
/// Presentation root, not under a submodule folder.
/// </summary>
public sealed class GetLedgerAccountEndpoint : IEndpoint
{
    public object Handle(GetLedgerAccountQuery query) => new GetLedgerAccountQueryHandler().Handle(query);

    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapGet("/ledger-accounts/{id}", () => Handle(new GetLedgerAccountQuery(Guid.Empty)));
}

/// <summary>
/// Deliberate violation: Presentation must never touch a data
/// framework directly — it maps requests to the mediator/Application layer only. Also a
/// composition violation: mapped directly at the Presentation root, not under a submodule folder.
/// </summary>
public sealed class PostLedgerEntryEndpoint(DbContext dbContext) : IEndpoint
{
    public void Handle() => _ = dbContext;

    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapPost("/ledger-entries", Handle);
}
