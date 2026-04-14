namespace TukiFact.Domain.Entities;

/// <summary>
/// Registro de cada email enviado por la plataforma.
/// Permite tracking, reintentos, y auditoría.
/// </summary>
public class EmailLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Template { get; set; } = "generic"; // document_emitted, reset_password, welcome, etc.

    // Status
    public string Status { get; set; } = "pending"; // pending, sent, failed, bounced
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }

    // Provider tracking
    public string? ExternalId { get; set; } // ID del proveedor (Resend, SES, etc.)
    public string Provider { get; set; } = "log"; // log, resend, smtp, ses

    // Related document (optional)
    public Guid? DocumentId { get; set; }

    // Timestamps
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
