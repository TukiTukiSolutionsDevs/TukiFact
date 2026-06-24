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
    // Recurrent endpoints: POST uses /create suffix; GET/PATCH/DELETE use the base path with id.
    private const string PlansCreatePath = "v2/recurrent/plans/create";
    private const string SubscriptionsCreatePath = "v2/recurrent/subscriptions/create";
    private const string SubscriptionsBasePath = "v2/recurrent/subscriptions";

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
        string? phoneNumber, string countryCode,
        string? address, string? addressCity, CancellationToken ct = default)
    {
        var client = BuildClient();
        var payload = new
        {
            first_name = firstName,
            last_name = lastName,
            email,
            country_code = countryCode,
            // Culqi requires phone_number 6-14 chars, address 5-100 chars, and a non-empty address_city.
            phone_number = SanitizePhone(phoneNumber),
            address = SanitizeAddress(address),
            address_city = string.IsNullOrWhiteSpace(addressCity) ? "Lima" : addressCity!.Trim(),
        };

        try
        {
            return await PostAndReadIdAsync(client, CustomersPath, payload, ct);
        }
        catch (CulqiApiException ex) when (ex.StatusCode == 400 &&
            (ex.Message.Contains("registrado", StringComparison.OrdinalIgnoreCase) ||
             ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase)))
        {
            // Duplicate email — a previous (possibly failed) attempt already created the customer.
            // Reuse it instead of failing forever.
            var existing = await FindCustomerByEmailAsync(client, email, ct);
            if (existing is not null) return existing;
            throw;
        }
    }

    private async Task<string?> FindCustomerByEmailAsync(HttpClient client, string email, CancellationToken ct)
    {
        var url = $"{CustomersPath}?email={Uri.EscapeDataString(email)}&limit=1";
        using var resp = await client.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!doc.RootElement.TryGetProperty("data", out var data) ||
            data.ValueKind != JsonValueKind.Array || data.GetArrayLength() == 0)
            return null;
        return data[0].TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
    }

    private static string SanitizeAddress(string? raw)
    {
        var trimmed = (raw ?? string.Empty).Trim();
        if (trimmed.Length < 5) return "Direccion no especificada";
        return trimmed.Length > 100 ? trimmed[..100] : trimmed;
    }

    private static string SanitizePhone(string? raw)
    {
        var digits = new string((raw ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length < 6) return "999999999";
        return digits.Length > 14 ? digits[..14] : digits;
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
        // /v2/recurrent/plans/create payload per Culqi API spec:
        //   - currency: "PEN" or "USD" (NOT currency_code)
        //   - interval_unit_time: 1=day, 2=week, 3=month, 4=year, 5=quarter, 6=semester
        //   - interval_count: 0 = indefinido; we use 1 (every month)
        //   - initial_cycles: required object; count=0 means no special intro period
        //   - duration is NOT a valid field
        var payload = new
        {
            name = $"TukiFact {planName}",
            short_name = planName.Length > 50 ? planName[..50] : planName,
            description = $"Plan mensual TukiFact {planName}",
            amount = amountCents,
            currency = "PEN",
            interval_unit_time = IntervalUnitMonthly,
            interval_count = 1,
            initial_cycles = new
            {
                count = 0,
                has_initial_charge = false,
                amount = 0,
                interval_unit_time = IntervalUnitMonthly,
            },
            metadata = new { tukifact_plan_id = plan.Id.ToString() },
        };
        var culqiPlanId = await PostAndReadIdAsync(client, PlansCreatePath, payload, ct);

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
            // Terms-and-conditions acceptance — implicit when user clicks Suscribirme on /plan.
            ["tyc"] = true,
        };
        if (metadata is { Count: > 0 })
            payload["metadata"] = metadata;
        return await PostAndReadIdAsync(client, SubscriptionsCreatePath, payload, ct);
    }

    public async Task CancelSubscriptionAsync(string culqiSubscriptionId, CancellationToken ct = default)
    {
        var client = BuildClient();
        using var response = await client.DeleteAsync($"{SubscriptionsBasePath}/{culqiSubscriptionId}", ct);
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
