using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using System.Net.Http;

namespace TukiFact.Infrastructure.Services;

public class WebhookDeliveryService
{
    private readonly IWebhookRepository _webhookRepo;
    private readonly IWebhookDeliveryRepository _deliveryRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDeliveryService> _logger;

    public WebhookDeliveryService(
        IWebhookRepository webhookRepo,
        IWebhookDeliveryRepository deliveryRepo,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDeliveryService> logger)
    {
        _webhookRepo = webhookRepo;
        _deliveryRepo = deliveryRepo;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task DeliverEventAsync(Guid tenantId, string eventType, object payload, CancellationToken ct = default)
    {
        var webhooks = await _webhookRepo.GetActiveByEventAsync(tenantId, eventType, ct);
        if (webhooks.Count == 0) return;

        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        foreach (var webhook in webhooks)
        {
            var delivery = new WebhookDelivery
            {
                TenantId = tenantId,
                WebhookConfigId = webhook.Id,
                EventType = eventType,
                Payload = payloadJson,
                Status = "pending"
            };
            await _deliveryRepo.CreateAsync(delivery, ct);

            _ = Task.Run(async () =>
            {
                for (int attempt = 1; attempt <= webhook.MaxRetries; attempt++)
                {
                    try
                    {
                        var client = _httpClientFactory.CreateClient("Webhook");
                        var signature = ComputeHmac(payloadJson, webhook.Secret);

                        var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
                        {
                            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                        };
                        request.Headers.Add("X-TukiFact-Event", eventType);
                        request.Headers.Add("X-TukiFact-Signature", signature);
                        request.Headers.Add("X-TukiFact-Delivery", delivery.Id.ToString());

                        var response = await client.SendAsync(request);
                        var responseBody = await response.Content.ReadAsStringAsync();

                        delivery.ResponseStatus = ((int)response.StatusCode).ToString();
                        delivery.ResponseBody = responseBody.Length > 1000 ? responseBody[..1000] : responseBody;
                        delivery.Attempt = attempt;
                        delivery.Status = response.IsSuccessStatusCode ? "delivered" : "failed";
                        await _deliveryRepo.UpdateAsync(delivery);

                        if (response.IsSuccessStatusCode)
                        {
                            webhook.LastTriggeredAt = DateTimeOffset.UtcNow;
                            await _webhookRepo.UpdateAsync(webhook);
                            _logger.LogInformation("Webhook delivered: {Event} to {Url}", eventType, webhook.Url);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        delivery.Attempt = attempt;
                        delivery.Status = "failed";
                        delivery.ResponseBody = ex.Message;
                        await _deliveryRepo.UpdateAsync(delivery);
                        _logger.LogWarning(ex, "Webhook delivery attempt {Attempt} failed for {Url}", attempt, webhook.Url);
                    }

                    if (attempt < webhook.MaxRetries)
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
                }
            }, ct);
        }
    }

    private static string ComputeHmac(string payload, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(key);
        return $"sha256={Convert.ToHexString(hmac.ComputeHash(data)).ToLowerInvariant()}";
    }
}
