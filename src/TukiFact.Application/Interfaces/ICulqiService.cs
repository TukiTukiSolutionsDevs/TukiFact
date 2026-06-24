namespace TukiFact.Application.Interfaces;

/// <summary>
/// Thin wrapper over Culqi v2 REST API (https://api.culqi.com/v2).
/// We never touch raw card data — the frontend tokenizes via Culqi.js
/// and only the opaque token (tkn_test_xxx) reaches us.
/// </summary>
public interface ICulqiService
{
    Task<string> CreateCustomerAsync(string email, string firstName, string lastName,
        string? phoneNumber, string countryCode,
        string? address, string? addressCity, CancellationToken ct = default);

    Task<string> CreateCardAsync(string customerId, string token, CancellationToken ct = default);

    /// <summary>
    /// Returns the Culqi plan id, creating it on Culqi if missing. Idempotent by name.
    /// Caller persists the returned id on Plan.CulqiPlanId.
    /// </summary>
    Task<string> EnsurePlanAsync(string planName, decimal monthlyAmountPen, CancellationToken ct = default);

    Task<string> CreateSubscriptionAsync(string cardId, string culqiPlanId,
        IReadOnlyDictionary<string, string>? metadata = null, CancellationToken ct = default);

    Task CancelSubscriptionAsync(string culqiSubscriptionId, CancellationToken ct = default);

    /// <summary>
    /// Validate webhook payload using HMAC-SHA256 with the private key as secret.
    /// Returns true if the signature matches the raw body bytes.
    /// </summary>
    bool VerifyWebhookSignature(byte[] rawBody, string? signatureHeader);
}

public class CulqiApiException : Exception
{
    public int StatusCode { get; }
    public string? ErrorCode { get; }

    public CulqiApiException(int statusCode, string? errorCode, string message, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
