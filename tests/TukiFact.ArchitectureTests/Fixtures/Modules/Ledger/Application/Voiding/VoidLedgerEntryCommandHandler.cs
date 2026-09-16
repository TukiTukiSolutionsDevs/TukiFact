using TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Application;

namespace TukiFact.ArchitectureTests.Fixtures.Modules.Ledger.Application.Voiding;

/// <summary>
/// Deliberate violation: a handler must sit in the same namespace as
/// its request; <see cref="VoidLedgerEntryCommand"/> lives one folder up, in the Application root.
/// Internal sealed and port-free, so only the placement axis fails here.
/// </summary>
internal sealed class VoidLedgerEntryCommandHandler
{
    public void Handle(VoidLedgerEntryCommand command) => _ = command;
}
