using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Infrastructure.Services;

public class CulqiService : ICulqiService
{
    private const string CustomersPath = "v2/customers";
    private const string CardsPath = "v2/cards";
    private const string PlansPath = "v2/recurrent/plans";
    private const string SubscriptionsPath = "v2/recurrent/subscriptions";

    // interval_unit_time per Culqi recurrent API: 1=day, 2=week, 3=month, 4=year.
    // If Culqi sandbox rejects this, only this constant needs to change.
    private const int IntervalUnitMonthly = 3;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _db;
    private readonly string? _secretKey;
    private readonly ILogger<CulqiService> _logger;

    public CulqiService(
        IHttpClientFactory httpClientFactory,
        AppDbContext db,
        IConfiguration configuration,
        ILogger<CulqiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _db = db;
        _secretKey = configuration["Culqi:SecretKey"];
        _logger = logger;
    }

    private HttpClient BuildClient()
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
            throw new InvalidOperationException(
                "Culqi:SecretKey no está configurado. Seteá la env Culqi__SecretKey antes de cobrar.");

        var client = _httpClientFactory.CreateClient("Culqi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
        return client;
    }

    public async Task<string> CreateCustomerAsync(string email, string firstName, string lastName,
        string? phoneNumber, string countryCode, CancellationToken ct = default)
    {
        var client = BuildClient();
        var payload = new
        {
            first_name = firstName,
            last_name = lastName,
            email,
            country_code = countryCode,
            phone_number = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber,
            address = "N/D",
            address_city = "Lima",
        };
        return await PostAndReadIdAsync(client, CustomersPath, payload, ct);
    }

    public async Task<string> CreateCardAsync(string customerId, string token, CancellationToken ct = default)
    {
        var client = BuildClient();
        var payload = new
        {
            customer_id = customerId,
            token_id = token,
            validate = false,
        };
        return await PostAndReadIdAsync(client, CardsPath, payload, ct);
    }

    public async Task<string> EnsurePlanAsync(string planName, decimal monthlyAmountPen, CancellationToken ct = default)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Name == planName, ct)
            ?? throw new InvalidOperationException($"Plan '{planName}' no existe en DB.");

        if (!string.IsNullOrEmpty(plan.CulqiPlanId))
            return plan.CulqiPlanId;

        // Amount in centimes per Culqi convention (PEN 70.00 -> 7000).
        var amountCents = (int)Math.Round(monthlyAmountPen * 100m, MidpointRounding.AwayFromZero);

        var client = BuildClient();
        var payload = new
        {
            name = $"TukiFact {planName}",
            short_name = planName.Length > 20 ? planName[..20] : planName,
            description = $"Plan mensual TukiFact {planName}",
            amount = amountCents,
            currency_code = "PEN",
            interval_unit_time = IntervalUnitMonthly,
            interval_count = 1,
            duration = 0,  // 0 = open-ended; cancel from our side
            metadata = new { tukifact_plan_id = plan.Id.ToString() },
        };
        var culqiPlanId = await PostAndReadIdAsync(client, PlansPath, payload, ct);

        plan.CulqiPlanId = culqiPlanId;
        await _db.SaveChangesAsync(ct);
        return culqiPlanId;
    }

    public async Task<string> CreateSubscriptionAsync(string cardId, string culqiPlanId,
        IReadOnlyDictionary<string, string>? metadata = null, CancellationToken ct = default)
    {
        var client = BuildClient();
        var payload = new Dictionary<string, object>
        {
            ["card_id"] = cardId,
            ["plan_id"] = culqiPlanId,
        };
        if (metadata is { Count: > 0 })
            payload["metadata"] = metadata;
        return await PostAndReadIdAsync(client, SubscriptionsPath, payload, ct);
    }

    public async Task CancelSubscriptionAsync(string culqiSubscriptionId, CancellationToken ct = default)
    {
        var client = BuildClient();
        using var response = await client.DeleteAsync($"{SubscriptionsPath}/{culqiSubscriptionId}", ct);
        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, ct);
    }

    public bool VerifyWebhookSignature(byte[] rawBody, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_secretKey) || string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var hash = hmac.ComputeHash(rawBody);
        var expected = Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(signatureHeader.Trim().ToLowerInvariant()));
    }

    private async Task<string> PostAndReadIdAsync(HttpClient client, string path, object payload, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(path, payload, ct);
        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, ct);

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!doc.RootElement.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.String)
            throw new CulqiApiException((int)response.StatusCode, null,
                $"Culqi {path} respondió sin id: {doc.RootElement}");
        return idEl.GetString()!;
    }

    private async Task<CulqiApiException> CreateExceptionAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        string? errorCode = null;
        string message = body;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("merchant_message", out var m))
                message = m.GetString() ?? message;
            if (doc.RootElement.TryGetProperty("type", out var t))
                errorCode = t.GetString();
        }
        catch (JsonException) { /* keep raw body */ }
        _logger.LogWarning("Culqi error {Status}: {Message}", (int)response.StatusCode, message);
        return new CulqiApiException((int)response.StatusCode, errorCode, message);
    }
}
