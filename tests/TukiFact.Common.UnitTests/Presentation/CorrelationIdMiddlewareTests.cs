using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Presentation.DependencyInjection;

namespace TukiFact.Common.UnitTests.Presentation;

/// <summary>
/// Exercises <c>CorrelationIdMiddleware</c> through a real in-memory pipeline:
/// reads or generates <c>X-Correlation-Id</c>, echoes it on the response, and it reaches
/// the ProblemDetails written for a failing endpoint.
/// </summary>
public sealed class CorrelationIdMiddlewareTests : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;

    public CorrelationIdMiddlewareTests()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(builder => builder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddCommonPresentation();
                    // Common.Presentation cannot reference Common.Infrastructure (where the real
                    // CorrelationIdAccessor lives), so the test supplies its own scoped fake — the
                    // host wires the real one in Program.cs (AddCommonPersistence).
                    services.AddScoped<ICorrelationIdAccessor, FakeCorrelationIdAccessor>();
                })
                .Configure(app =>
                {
                    app.UseExceptionHandler();
                    app.UseStatusCodePages();
                    app.UseCorrelationId();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        // Fully qualified: the sibling TukiFact.Common.UnitTests.Results namespace
                        // (ResultExtensionsTests.cs) shadows Microsoft.AspNetCore.Http.Results here.
                        endpoints.MapGet("/echo", (ICorrelationIdAccessor correlation) =>
                            Microsoft.AspNetCore.Http.Results.Text(correlation.CorrelationId));
                        endpoints.MapGet("/fail", () => Microsoft.AspNetCore.Http.Results.Problem(statusCode: 404));
                    });
                }))
            .Build();
        _host.Start();
        _client = _host.GetTestServer().CreateClient();
    }

    [Fact]
    public async Task InvokeAsync_NoIncomingHeader_GeneratesOneAndEchoesItOnTheResponse()
    {
        // Act
        var response = await _client.GetAsync("/echo", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.Headers.Should().ContainKey(ICorrelationIdAccessor.HeaderName);
        var echoed = response.Headers.GetValues(ICorrelationIdAccessor.HeaderName).Single();
        echoed.Should().NotBeNullOrWhiteSpace();
        body.Should().Be(echoed);
    }

    [Fact]
    public async Task InvokeAsync_WellFormedIncomingHeader_IsKeptAndEchoed()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "/echo");
        request.Headers.Add(ICorrelationIdAccessor.HeaderName, "req-abc-123");

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.Headers.GetValues(ICorrelationIdAccessor.HeaderName).Single().Should().Be("req-abc-123");
        body.Should().Be("req-abc-123");
    }

    [Fact]
    public async Task InvokeAsync_MalformedIncomingHeader_IsIgnoredAndReplacedWithAGeneratedId()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "/echo");
        request.Headers.Add(ICorrelationIdAccessor.HeaderName, "has spaces / slash");

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        var echoed = response.Headers.GetValues(ICorrelationIdAccessor.HeaderName).Single();
        echoed.Should().NotBe("has spaces / slash");
        body.Should().Be(echoed);
    }

    [Fact]
    public async Task InvokeAsync_FailingEndpoint_ProblemDetailsCarriesTheCorrelationId()
    {
        // The exception-handler pipeline clears the response before writing ProblemDetails;
        // OnStarting must survive that so the header still reaches error responses too.
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "/fail");
        request.Headers.Add(ICorrelationIdAccessor.HeaderName, "req-fail-456");

        // Act
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.Headers.GetValues(ICorrelationIdAccessor.HeaderName).Single().Should().Be("req-fail-456");
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        _host.Dispose();
        await ValueTask.CompletedTask;
    }

    /// <summary>Test-only stand-in for the real (internal, Infrastructure-owned) CorrelationIdAccessor.</summary>
    private sealed class FakeCorrelationIdAccessor : ICorrelationIdAccessor
    {
        private string? _correlationId;

        public string CorrelationId => _correlationId ??= Guid.NewGuid().ToString("N");

        public void Set(string correlationId) => _correlationId = correlationId;
    }
}
