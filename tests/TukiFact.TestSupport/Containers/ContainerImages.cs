namespace TukiFact.TestSupport.Containers;

/// <summary>Images of the Testcontainers dependencies the kernel integration tests need.</summary>
public static class ContainerImages
{
    /// <summary>Same major version as the production image, <c>postgres:18</c> (docker/docker-compose.prod.yml).</summary>
    public const string Postgres = "postgres:18-alpine";
}
