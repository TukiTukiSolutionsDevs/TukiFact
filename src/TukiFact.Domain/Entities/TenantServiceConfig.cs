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

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
