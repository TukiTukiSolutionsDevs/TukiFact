namespace TukiFact.ArchitectureTests.Solution;

/// <summary>The naming scheme every rule classifies a project by.</summary>
public static class ProjectNaming
{
    public const string CommonPrefix = "TukiFact.Common.";
    public const string ModulePrefix = "TukiFact.Modules.";
    public const string ContractsName = "TukiFact.BuildingBlocks.Contracts";

    /// <summary>
    /// Generated gRPC contracts: compiles <c>contracts/tukifact/v1/*.proto</c>.
    /// Unlike <see cref="ContractsName"/> it is allowed PackageReferences (Grpc.Tools, Google.Protobuf) —
    /// it is allowlisted as generated code, exempt from convention rules, but still bound by dependency rules.
    /// </summary>
    public const string GeneratedContractsName = "TukiFact.Contracts.Grpc";

    public const string HostName = "TukiFact.Api";

    /// <summary>Exact set — asserted by <c>LegacyAllowlistTests</c> (ADR-011); nothing is ever added to this list.</summary>
    public static readonly IReadOnlyList<string> LegacyProjects =
    [
        "TukiFact.Api",
        "TukiFact.Application",
        "TukiFact.Domain",
        "TukiFact.Infrastructure",
    ];
}
