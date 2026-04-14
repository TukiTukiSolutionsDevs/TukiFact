namespace TukiFact.Domain.Entities;

/// <summary>
/// Stores per-tenant configuration for external services.
/// Each tenant can configure their own API keys for:
/// - Data lookup providers (DNI/RUC: apiperu.dev, api.migo.pe, peruapi.com)
/// - AI/LLM providers (Gemini, Claude, Grok, DeepSeek, OpenAI)
/// TukiFact acts as a pure invoicing platform — external services
/// are paid and configured by each tenant independently.
/// </summary>
public class TenantServiceConfig
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    // --- Data Lookup Provider (DNI/RUC) ---
    /// <summary>Lookup provider identifier: "apiperu", "migo", "peruapi", "apis_net", "none"</summary>
    public string LookupProvider { get; set; } = "none";
    /// <summary>Bearer token or API key for the lookup provider (encrypted at rest)</summary>
    public string? LookupApiKey { get; set; }

    // --- AI/LLM Provider ---
    /// <summary>AI provider identifier: "gemini", "claude", "grok", "deepseek", "openai", "none"</summary>
    public string AiProvider { get; set; } = "none";
    /// <summary>API key for the AI provider (encrypted at rest)</summary>
    public string? AiApiKey { get; set; }
    /// <summary>Model to use: "gemini-2.5-flash", "claude-sonnet-4-20250514", "grok-3", "deepseek-chat", etc.</summary>
    public string? AiModel { get; set; }

    // --- Email Config ---
    /// <summary>Auto-send email when document is emitted</summary>
    public bool AutoSendEmail { get; set; } = false;
    /// <summary>Email provider: "log" (stub), "resend", "smtp"</summary>
    public string EmailProvider { get; set; } = "log";
    /// <summary>Resend API key (if provider=resend)</summary>
    public string? ResendApiKey { get; set; }
    /// <summary>SMTP host (if provider=smtp)</summary>
    public string? SmtpHost { get; set; }
    /// <summary>SMTP port (if provider=smtp)</summary>
    public int? SmtpPort { get; set; }
    /// <summary>SMTP user (if provider=smtp)</summary>
    public string? SmtpUser { get; set; }
    /// <summary>SMTP password (if provider=smtp, encrypted)</summary>
    public string? SmtpPassword { get; set; }
    /// <summary>From name: "Mi Empresa SAC"</summary>
    public string? EmailFromName { get; set; }
    /// <summary>From address: "facturacion@miempresa.com"</summary>
    public string? EmailFromAddress { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
