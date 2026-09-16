using System.Net;
using FluentAssertions;
using TukiFact.Api.Tests.Fixtures;

namespace TukiFact.Api.Tests;

/// <summary>
/// The host mounts the kernel's <c>MapEndpoints()</c> versioned group (empty module catalog) alongside
/// the 33 legacy MVC controllers: an unmatched versioned route must
/// still 404 like any unknown route, not collide with the legacy routing table or crash the pipeline.
/// </summary>
[Collection("Postgres")]
public sealed class RouteCollisionTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private TukiFactAppFactory _factory = null!;
    private HttpClient _client = null!;

    public RouteCollisionTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    public Task InitializeAsync()
    {
        _factory = new TukiFactAppFactory(_postgres.ConnectionString);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return _factory.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task Get_UnknownVersionedRoute_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/anything");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
