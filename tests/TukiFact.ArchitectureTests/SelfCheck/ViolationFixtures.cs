using TukiFact.ArchitectureTests.Rules;
using TukiFact.ArchitectureTests.Solution;

namespace TukiFact.ArchitectureTests.SelfCheck;

/// <summary>
/// Scopes over the deliberate violations under <c>Fixtures/Modules</c>:
/// <c>Ledger</c> breaks every type-level dependency rule once, <c>Freight</c> is clean and is
/// the target of Ledger's forbidden cross-module dependency. They exist only in this test assembly,
/// so production scanning (<see cref="ProductionSolution"/>) never sees them.
/// </summary>
internal static class ViolationFixtures
{
    public const string ModulesNamespaceRoot = "TukiFact.ArchitectureTests.Fixtures.Modules";

    private static readonly string[] FixtureModules = ["Ledger", "Freight"];

    public static LayerScope Ledger(Layer layer) => Scope("Ledger", layer);

    public static LayerScope Freight(Layer layer) => Scope("Freight", layer);

    private static LayerScope Scope(string module, Layer layer) =>
        new(
            typeof(ViolationFixtures).Assembly,
            $"{ModulesNamespaceRoot}.{module}.{layer}",
            layer,
            module,
            ModulesNamespaceRoot,
            [.. FixtureModules.Where(other => other != module)]);
}
