namespace TukiFact.Common.IntegrationTests.Persistence;

/// <summary>One Postgres container, one schema and one probe-role/RLS setup for the whole collection.</summary>
[CollectionDefinition(Name)]
public sealed class KernelPostgresCollection : ICollectionFixture<KernelTestFixture>
{
    public const string Name = "KernelPostgres";
}
