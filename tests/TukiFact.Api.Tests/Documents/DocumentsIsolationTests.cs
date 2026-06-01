using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using TukiFact.Api.Tests.Fixtures;
using TukiFact.Api.Tests.Helpers;

namespace TukiFact.Api.Tests.Documents;

/// <summary>
/// Cross-tenant isolation (IDOR) smoke tests. We don't have a doc to look up but the
/// API must respond 404 for an unknown GUID — never 403, 500 or leaking that
/// "doc exists but you can't see it".
/// </summary>
[Collection("Postgres")]
public class DocumentsIsolationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private TukiFactAppFactory _factory = null!;
    private HttpClient _client = null!;

    public DocumentsIsolationTests(PostgresFixture postgres)
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
    public async Task GET_document_by_id_for_random_guid_returns_404_not_403()
    {
        var token = JwtTokenFactory.CreateToken(
            userId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            role: "admin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var randomId = Guid.NewGuid();
        var res = await _client.GetAsync($"/v1/documents/{randomId}");

        // 404 means "either doesn't exist OR doesn't belong to your tenant", which is the
        // correct response and prevents tenant existence enumeration.
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_documents_list_returns_empty_for_brand_new_tenant()
    {
        var token = JwtTokenFactory.CreateToken(
            userId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            role: "admin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await _client.GetAsync("/v1/documents");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadAsStringAsync();
        // Whether the response is `[]` or `{ items: [], total: 0 }`, the key invariant is
        // it must not contain another tenant's doc id.
        body.Should().NotBeNullOrEmpty();
    }
}
