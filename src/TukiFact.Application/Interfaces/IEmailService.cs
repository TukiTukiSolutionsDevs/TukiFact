namespace TukiFact.Application.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Send a document (factura/boleta/NC/ND) by email with PDF attached.
    /// </summary>
    Task SendDocumentEmailAsync(Guid tenantId, Guid documentId, string recipientEmail, CancellationToken ct = default);

    /// <summary>
    /// Send a generic email with optional attachments.
    /// </summary>
    Task<EmailResult> SendAsync(EmailMessage message, CancellationToken ct = default);
}

public class EmailMessage
{
    public string From { get; set; } = string.Empty;
    public string FromName { get; set; } = "TukiFact";
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public List<EmailAttachment> Attachments { get; set; } = new();
    public string? ReplyTo { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? DocumentId { get; set; }
    public string Template { get; set; } = "generic";
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/pdf";
}

public record EmailResult(
    bool Success,
    string? ExternalId,
    string? ErrorMessage
);
