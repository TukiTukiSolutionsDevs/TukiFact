using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TukiFact.Api.Tests.Fixtures;
using TukiFact.Api.Tests.Helpers;
using TukiFact.Application.DTOs.Documents;

namespace TukiFact.Api.Tests.Documents;

/// <summary>
/// Auth + smoke tests for /v1/documents. These don't require seed data beyond what
/// DataSeeder creates at startup (Plans + default admin tenant/user).
/// </summary>
[Collection("Postgres")]
public class DocumentsAuthTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private TukiFactAppFactory _factory = null!;
    private HttpClient _client = null!;

    public DocumentsAuthTests(PostgresFixture postgres)
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
    public async Task Health_endpoint_returns_200_proving_app_boots()
    {
        var res = await _client.GetAsync("/health/live");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_documents_without_authorization_returns_401()
    {
        var res = await _client.PostAsJsonAsync("/v1/documents", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_documents_with_invalid_jwt_returns_401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not-a-real-jwt");

        var res = await _client.PostAsJsonAsync("/v1/documents", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_documents_with_valid_jwt_but_empty_body_returns_400_with_details()
    {
        var token = JwtTokenFactory.CreateToken(
            userId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            role: "admin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Send a structurally complete but semantically empty payload so the JSON
        // deserializer succeeds and the validator (#4 in roadmap) is reached.
        var emptyRequest = new CreateDocumentRequest(
            DocumentType: "",
            Serie: "",
            IssueDate: null,
            DueDate: null,
            Currency: "",
            CustomerDocType: "",
            CustomerDocNumber: "",
            CustomerName: "",
            CustomerAddress: null,
            CustomerEmail: null,
            Notes: null,
            PurchaseOrder: null,
            Items: new List<CreateDocumentItemRequest>());

        var res = await _client.PostAsJsonAsync("/v1/documents", emptyRequest);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("inválidos", "the validator returns Spanish error messages")
            .And.Contain("\"details\"", "the validator returns a list of errors");
    }
}
