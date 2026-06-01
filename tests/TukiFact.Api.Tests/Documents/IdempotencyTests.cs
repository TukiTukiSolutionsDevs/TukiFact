using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TukiFact.Api.Tests.Fixtures;
using TukiFact.Api.Tests.Helpers;
using TukiFact.Application.DTOs.Documents;

namespace TukiFact.Api.Tests.Documents;

/// <summary>
/// Behavioural tests for the Idempotency-Key middleware (#3 in roadmap).
/// We don't need a successful SUNAT call — the middleware sits in front of the
/// controller and must short-circuit duplicate POSTs by content hash.
/// </summary>
[Collection("Postgres")]
public class IdempotencyTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private TukiFactAppFactory _factory = null!;
    private HttpClient _client = null!;

    public IdempotencyTests(PostgresFixture postgres)
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

    // Replay/conflict tests (same key + same body → replay header; same key + different
    // body → 409) need a successful first POST so the middleware persists the response
    // row. Those require mocking ISunatClient + ISunatSigner to bypass the real SUNAT
    // call, which is out of scope for this initial test slice. Documented as TODO.

    [Fact]
    public async Task No_key_means_no_replay_no_conflict()
    {
        var token = JwtTokenFactory.CreateToken(
            userId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            role: "admin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var first = await _client.PostAsJsonAsync("/v1/documents", MakeRequest());
        var second = await _client.PostAsJsonAsync("/v1/documents", MakeRequest());

        // Both 400, neither carries replay header (middleware passes through).
        first.Headers.Contains("X-Idempotent-Replay").Should().BeFalse();
        second.Headers.Contains("X-Idempotent-Replay").Should().BeFalse();
    }

    private async Task<HttpResponseMessage> PostWithKey(string path, CreateDocumentRequest body, string key)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body),
        };
        req.Headers.Add("Idempotency-Key", key);
        return await _client.SendAsync(req);
    }

    private static CreateDocumentRequest MakeRequest(string customerName = "Cliente Prueba")
        => new(
            DocumentType: "01",
            Serie: "F001",
            IssueDate: null,
            DueDate: null,
            Currency: "PEN",
            CustomerDocType: "6",
            CustomerDocNumber: "20613614509",
            CustomerName: customerName,
            CustomerAddress: null,
            CustomerEmail: null,
            Notes: null,
            PurchaseOrder: null,
            Items: new List<CreateDocumentItemRequest>
            {
                new(
                    ProductCode: null,
                    SunatProductCode: null,
                    Description: "Servicio",
                    Quantity: 1,
                    UnitMeasure: "NIU",
                    UnitPrice: 100,
                    IgvType: "10"),
            });
}
