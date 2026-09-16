namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

/// <summary>Positional read model shaped like a Dapper query response (constructor mapping).</summary>
public sealed record KernelTestOrderRow(
    Guid Id,
    Guid TenantId,
    string Reference,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy,
    long SyncVersion);
