using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TukiFact.Api.Tests.Fixtures;
using TukiFact.Api.Tests.Helpers;

namespace TukiFact.Api.Tests.Documents;

/// <summary>
/// Theory-driven auth + validation tests across the 4 SUNAT emit flows
/// (Documents / Perceptions / Retentions / Voided). Verifies the security and
/// validator wiring (#4 in roadmap) is consistent across endpoints.
/// </summary>
[Collection("Postgres")]
public class SunatFlowAuthTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private TukiFactAppFactory _factory = null!;
    private HttpClient _client = null!;

    public SunatFlowAuthTests(PostgresFixture postgres)
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

    public static IEnumerable<object[]> SunatEmitEndpoints => new[]
    {
        new object[] { "/v1/documents" },
        new object[] { "/v1/perceptions" },
        new object[] { "/v1/retentions" },
        new object[] { "/v1/voided-documents" },
    };

    [Theory]
    [MemberData(nameof(SunatEmitEndpoints))]
    public async Task POST_without_authorization_returns_401(string path)
    {
        var res = await _client.PostAsJsonAsync(path, new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: $"{path} must require a Bearer token");
    }

    [Theory]
    [MemberData(nameof(SunatEmitEndpoints))]
    public async Task POST_with_empty_body_returns_400_with_validation_details(string path)
    {
        var token = JwtTokenFactory.CreateToken(
            userId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            role: "admin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Empty object — every field will fail the validator's required-field checks.
        var res = await _client.PostAsJsonAsync(path, new { });

        // 400 is the contract; 500 would mean validation threw instead of returning a
        // structured error.
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: $"{path} must answer 400 (not 500) for an empty/invalid body");

        var body = await res.Content.ReadAsStringAsync();
        // Either the framework's ModelState `errors{}` (for null required fields) or the
        // custom validator's `details[]` (for semantic violations). Both prove validation
        // is wired; we just don't want a raw 500 or empty body.
        var hasValidation = body.Contains("\"errors\"") || body.Contains("\"details\"");
        hasValidation.Should().BeTrue(
            because: $"{path} should return a structured validation error, got: {body}");
    }
}
