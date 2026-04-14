using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Infrastructure.Services;

/// <summary>
/// Email service with multiple provider support:
/// - "log" = stub (logs to console, no real send)
/// - "resend" = Resend API (recommended for MVP)
/// - "smtp" = Generic SMTP (MailKit/SmtpClient)
/// Provider is configured per-tenant in TenantServiceConfig.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IDocumentRepository _documentRepo;
    private readonly ITenantRepository _tenantRepo;
    private readonly IPdfGenerator _pdfGenerator;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<EmailService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _defaultProvider;
    private readonly string _defaultFromAddress;

    public EmailService(
        IDocumentRepository documentRepo, ITenantRepository tenantRepo,
        IPdfGenerator pdfGenerator, AppDbContext dbContext,
        ILogger<EmailService> logger, IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _documentRepo = documentRepo;
        _tenantRepo = tenantRepo;
        _pdfGenerator = pdfGenerator;
        _dbContext = dbContext;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _defaultProvider = configuration["Email:Provider"] ?? "log";
        _defaultFromAddress = configuration["Email:FromAddress"] ?? "noreply@tukifact.net.pe";
    }

    public async Task SendDocumentEmailAsync(Guid tenantId, Guid documentId, string recipientEmail, CancellationToken ct = default)
    {
        var document = await _documentRepo.GetByIdWithItemsAsync(documentId, ct);
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct);
        if (document is null || tenant is null) return;

        var pdfBytes = _pdfGenerator.GenerateInvoicePdf(document, tenant);

        var typeName = document.DocumentType switch
        {
            "01" => "Factura Electrónica",
            "03" => "Boleta de Venta Electrónica",
            "07" => "Nota de Crédito Electrónica",
            "08" => "Nota de Débito Electrónica",
            _ => "Comprobante Electrónico"
        };

        var message = new EmailMessage
        {
            From = tenant.ServiceConfig?.EmailFromAddress ?? _defaultFromAddress,
            FromName = tenant.ServiceConfig?.EmailFromName ?? tenant.RazonSocial,
            To = recipientEmail,
            Subject = $"{typeName} {document.FullNumber} — {tenant.RazonSocial}",
            HtmlBody = BuildDocumentEmailHtml(document, tenant, typeName),
            Template = "document_emitted",
            TenantId = tenantId,
            DocumentId = documentId,
            Attachments =
            {
                new EmailAttachment
                {
                    FileName = $"{document.FullNumber}.pdf",
                    Content = pdfBytes,
                    ContentType = "application/pdf"
                }
            }
        };

        await SendAsync(message, ct);
    }

    public async Task<EmailResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        // Determine provider (per-tenant config or global default)
        var provider = _defaultProvider;
        TenantServiceConfig? tenantConfig = null;

        if (message.TenantId.HasValue)
        {
            tenantConfig = await _dbContext.TenantServiceConfigs
                .FindAsync(new object?[] { null }, ct); // Find by TenantId via query
            // Actually query by TenantId
            tenantConfig = _dbContext.TenantServiceConfigs
                .FirstOrDefault(c => c.TenantId == message.TenantId.Value);

            if (tenantConfig is not null && tenantConfig.EmailProvider != "log")
                provider = tenantConfig.EmailProvider;
        }

        _logger.LogInformation("Sending email to {To} via {Provider} (template: {Template})",
            message.To, provider, message.Template);

        EmailResult result;
        try
        {
            result = provider switch
            {
                "resend" => await SendViaResendAsync(message, tenantConfig?.ResendApiKey, ct),
                "smtp" => await SendViaSmtpAsync(message, tenantConfig, ct),
                _ => await SendViaLogAsync(message, ct)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email send failed to {To}", message.To);
            result = new EmailResult(false, null, ex.Message);
        }

        // Log the email
        await LogEmailAsync(message, provider, result, ct);

        return result;
    }

    // === Resend Provider ===

    private async Task<EmailResult> SendViaResendAsync(EmailMessage message, string? apiKey, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Resend API key no configurada para este tenant");

        var client = _httpClientFactory.CreateClient("Resend");

        var payload = new
        {
            from = $"{message.FromName} <{message.From}>",
            to = new[] { message.To },
            cc = message.Cc is not null ? new[] { message.Cc } : null,
            subject = message.Subject,
            html = message.HtmlBody,
            reply_to = message.ReplyTo,
            attachments = message.Attachments.Select(a => new
            {
                filename = a.FileName,
                content = Convert.ToBase64String(a.Content),
                content_type = a.ContentType
            }).ToArray()
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Resend API failed: {Status} {Body}", response.StatusCode, body);
            return new EmailResult(false, null, $"Resend error: {response.StatusCode} - {body}");
        }

        var jsonDoc = JsonDocument.Parse(body);
        var externalId = jsonDoc.RootElement.TryGetProperty("id", out var idProp)
            ? idProp.GetString() : null;

        _logger.LogInformation("Email sent via Resend, id: {Id}", externalId);
        return new EmailResult(true, externalId, null);
    }

    // === SMTP Provider ===

    private async Task<EmailResult> SendViaSmtpAsync(EmailMessage message, TenantServiceConfig? config, CancellationToken ct)
    {
        if (config is null || string.IsNullOrEmpty(config.SmtpHost))
            throw new InvalidOperationException("SMTP no configurado para este tenant");

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(message.From, message.FromName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true
        };
        mailMessage.To.Add(message.To);

        if (message.Cc is not null)
            mailMessage.CC.Add(message.Cc);

        foreach (var attachment in message.Attachments)
        {
            var stream = new MemoryStream(attachment.Content);
            mailMessage.Attachments.Add(new Attachment(stream, attachment.FileName, attachment.ContentType));
        }

        using var smtpClient = new SmtpClient(config.SmtpHost, config.SmtpPort ?? 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(config.SmtpUser, config.SmtpPassword)
        };

        await smtpClient.SendMailAsync(mailMessage, ct);
        _logger.LogInformation("Email sent via SMTP to {To}", message.To);
        return new EmailResult(true, null, null);
    }

    // === Log Provider (stub) ===

    private Task<EmailResult> SendViaLogAsync(EmailMessage message, CancellationToken ct)
    {
        _logger.LogInformation(
            "EMAIL STUB: To={To} Subject={Subject} Attachments={Count} Template={Template}",
            message.To, message.Subject, message.Attachments.Count, message.Template);
        return Task.FromResult(new EmailResult(true, $"log-{Guid.NewGuid():N}"[..20], null));
    }

    // === Logging ===

    private async Task LogEmailAsync(EmailMessage message, string provider, EmailResult result, CancellationToken ct)
    {
        if (message.TenantId is null) return;

        var log = new EmailLog
        {
            TenantId = message.TenantId.Value,
            To = message.To,
            Cc = message.Cc,
            Subject = message.Subject,
            Template = message.Template,
            Status = result.Success ? "sent" : "failed",
            ErrorMessage = result.ErrorMessage,
            ExternalId = result.ExternalId,
            Provider = provider,
            DocumentId = message.DocumentId,
            SentAt = result.Success ? DateTimeOffset.UtcNow : null
        };

        await _dbContext.EmailLogs.AddAsync(log, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    // === HTML Templates ===

    private static string BuildDocumentEmailHtml(Document document, Tenant tenant, string typeName)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"></head>
        <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">
            <div style="background: #f8f9fa; border-radius: 8px; padding: 24px; margin-bottom: 20px;">
                <h2 style="margin: 0 0 8px 0; color: #1a1a2e;">{tenant.RazonSocial}</h2>
                <p style="margin: 0; color: #666; font-size: 14px;">RUC: {tenant.Ruc}</p>
            </div>

            <div style="padding: 20px 0;">
                <p>Estimado(a) <strong>{document.CustomerName}</strong>,</p>
                <p>Le informamos que se ha emitido el siguiente comprobante electrónico:</p>

                <table style="width: 100%; border-collapse: collapse; margin: 20px 0;">
                    <tr style="background: #f0f0f0;">
                        <td style="padding: 10px; font-weight: bold; border: 1px solid #ddd;">Tipo</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">{typeName}</td>
                    </tr>
                    <tr>
                        <td style="padding: 10px; font-weight: bold; border: 1px solid #ddd;">Número</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">{document.FullNumber}</td>
                    </tr>
                    <tr style="background: #f0f0f0;">
                        <td style="padding: 10px; font-weight: bold; border: 1px solid #ddd;">Fecha</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">{document.IssueDate:dd/MM/yyyy}</td>
                    </tr>
                    <tr>
                        <td style="padding: 10px; font-weight: bold; border: 1px solid #ddd;">Moneda</td>
                        <td style="padding: 10px; border: 1px solid #ddd;">{document.Currency}</td>
                    </tr>
                    <tr style="background: #f0f0f0;">
                        <td style="padding: 10px; font-weight: bold; border: 1px solid #ddd;">Total</td>
                        <td style="padding: 10px; border: 1px solid #ddd; font-size: 18px; font-weight: bold;">{document.Currency} {document.Total:N2}</td>
                    </tr>
                </table>

                <p>Adjunto encontrará el comprobante en formato PDF.</p>
                <p style="color: #666; font-size: 12px;">Este comprobante ha sido emitido electrónicamente y tiene validez tributaria según las disposiciones de SUNAT.</p>
            </div>

            <div style="border-top: 1px solid #eee; padding-top: 16px; margin-top: 20px;">
                <p style="color: #999; font-size: 12px; margin: 0;">
                    Emitido por <strong>{tenant.RazonSocial}</strong> mediante TukiFact.<br>
                    {(tenant.Direccion is not null ? $"{tenant.Direccion}<br>" : "")}
                    Este es un email automático, por favor no responda directamente.
                </p>
            </div>
        </body>
        </html>
        """;
    }
}
