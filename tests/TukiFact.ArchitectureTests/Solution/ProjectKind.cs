namespace TukiFact.ArchitectureTests.Solution;

/// <summary>Role of a project in the solution, derived from its name.</summary>
public enum ProjectKind
{
    Unknown,
    Contracts,

    /// <summary>Generated gRPC contracts (<see cref="ProjectNaming.GeneratedContractsName"/>): allowlisted, exempt from convention rules, still bound by dependency rules.</summary>
    GeneratedContracts,
    Common,
    Module,

    /// <summary>One of the four pre-existing monolith projects (<see cref="ProjectNaming.LegacyProjects"/>), including the host.</summary>
    Legacy,
}
