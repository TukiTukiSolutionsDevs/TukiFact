namespace TukiFact.Api.Hosting;

/// <summary>
/// The explicit module list of the host: no assembly scanning. Empty for
/// now — the kernel is wired but no module exists yet. A module adds one entry here (its
/// Presentation assembly, for <c>AddEndpoints</c>) once it has its own project and a first use case.
/// </summary>
public static class ModuleCatalog
{
    public static IReadOnlyCollection<System.Reflection.Assembly> EndpointAssemblies { get; } = [];
}
