using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TukiFact.Api.Tests.Fixtures;

/// <summary>
/// WebApplicationFactory wired to a Testcontainers-managed Postgres. Replaces the
/// default connection string and disables outbound emit-related services so that the
/// API boots without touching real SUNAT or external providers.
/// </summary>
public class TukiFactAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public TukiFactAppFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                // Use a deterministic JWT secret across test runs so JwtTokenFactory can sign tokens
                // that the API will accept. Must be at least 32 chars.
                ["Jwt:Secret"] = "TukiFact-Test-Secret-Key-That-Is-Long-Enough-2026!",
                ["Jwt:Issuer"] = "TukiFact",
                ["Jwt:Audience"] = "TukiFact",
                // Turn off Sentry, NATS, MinIO health checks for tests.
                ["Sentry:Dsn"] = string.Empty,
            });
        });

        builder.ConfigureServices(services =>
        {
            // Tests boot against a clean Postgres, the seeder will run via Program.cs.
            // No service overrides needed for the negative-path tests we're focusing on
            // (auth, validation, IDOR, idempotency). Happy-path / SUNAT tests would
            // register fake ISunatClient + ISunatSigner here.
            _ = services;
        });
    }
}
