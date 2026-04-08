using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Interfaces;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Api.Controllers;

/// <summary>
/// Manages per-tenant external service configurations (lookup providers, AI providers).
/// Each tenant brings their own API keys — TukiFact is a pure invoicing platform.
/// </summary>
[ApiController]
[Route("v1/services")]
[Authorize]
public class ExternalServicesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExternalServicesController> _logger;

    public ExternalServicesController(
        AppDbContext db,
        ITenantProvider tenantProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<ExternalServicesController> logger)
    {
        _db = db;
        _tenantProvider = tenantProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ====================================================
    // SERVICE CONFIG CRUD
    // ====================================================

    /// <summary>Get current tenant's external service configuration</summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var config = await _db.TenantServiceConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

        if (config is null)
            return Ok(new
            {
                lookupProvider = "none",
                lookupApiKeyConfigured = false,
                aiProvider = "none",
                aiApiKeyConfigured = false,
                aiModel = (string?)null
            });

        return Ok(new
        {
            lookupProvider = config.LookupProvider,
            lookupApiKeyConfigured = !string.IsNullOrEmpty(config.LookupApiKey),
            aiProvider = config.AiProvider,
            aiApiKeyConfigured = !string.IsNullOrEmpty(config.AiApiKey),
            aiModel = config.AiModel
        });
    }

    /// <summary>Update tenant's external service configuration (admin only)</summary>
    [HttpPut("config")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateServiceConfigRequest request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var config = await _db.TenantServiceConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

        if (config is null)
        {
            config = new TenantServiceConfig { TenantId = tenantId };
            _db.TenantServiceConfigs.Add(config);
        }

        // Only update fields that are provided (not null)
        if (request.LookupProvider is not null)
        {
            var validLookup = new[] { "none", "apiperu", "migo", "peruapi", "apis_net" };
            if (!validLookup.Contains(request.LookupProvider))
                return BadRequest(new { error = $"Proveedor de datos inválido. Opciones: {string.Join(", ", validLookup)}" });
            config.LookupProvider = request.LookupProvider;
        }

        if (request.LookupApiKey is not null)
            config.LookupApiKey = request.LookupApiKey; // TODO: encrypt at rest

        if (request.AiProvider is not null)
        {
            var validAi = new[] { "none", "gemini", "claude", "grok", "deepseek", "openai" };
            if (!validAi.Contains(request.AiProvider))
                return BadRequest(new { error = $"Proveedor de IA inválido. Opciones: {string.Join(", ", validAi)}" });
            config.AiProvider = request.AiProvider;
        }

        if (request.AiApiKey is not null)
            config.AiApiKey = request.AiApiKey; // TODO: encrypt at rest

        if (request.AiModel is not null)
            config.AiModel = request.AiModel;

        config.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Configuración actualizada" });
    }

    // ====================================================
    // AVAILABLE PROVIDERS (public info)
    // ====================================================

    /// <summary>List available external service providers</summary>
    [HttpGet("providers")]
    [AllowAnonymous]
    public IActionResult GetProviders()
    {
        return Ok(new
        {
            lookup = new[]
            {
                new { id = "apiperu", name = "ApiPeru.dev", url = "https://apiperu.dev", freeTier = "100/mes gratis", paidFrom = "S/15/mes" },
                new { id = "migo", name = "Migo API", url = "https://api.migo.pe", freeTier = "700 demo gratis", paidFrom = "S/15/mes" },
                new { id = "peruapi", name = "Perú API", url = "https://peruapi.com", freeTier = "Free tier", paidFrom = "Varía" },
                new { id = "apis_net", name = "APIs.net.pe", url = "https://apis.net.pe", freeTier = "Solo pago", paidFrom = "Varía" },
            },
            ai = new[]
            {
                new { id = "gemini", name = "Google Gemini", models = new[] {
                    "gemini-3.1-pro-preview", "gemini-3-flash",
                    "gemini-2.5-pro", "gemini-2.5-flash",
                } },
                new { id = "claude", name = "Anthropic Claude", models = new[] {
                    "claude-sonnet-4-6", "claude-opus-4-6",
                    "claude-sonnet-4-5-20250929", "claude-opus-4-5-20251101",
                    "claude-haiku-4-5-20251001",
                    "claude-sonnet-4-20250514",
                } },
                new { id = "grok", name = "xAI Grok", models = new[] {
                    "grok-4.20", "grok-4.1",
                    "grok-3", "grok-3-mini",
                } },
                new { id = "deepseek", name = "DeepSeek", models = new[] {
                    "deepseek-chat", "deepseek-reasoner",
                } },
                new { id = "openai", name = "OpenAI", models = new[] {
                    "gpt-5.3-codex", "gpt-5.2",
                    "gpt-5", "gpt-5-mini",
                    "gpt-4.1", "gpt-4.1-mini",
                    "o3", "o3-mini",
                } },
            }
        });
    }

    // ====================================================
    // LOOKUP PROXY (DNI / RUC)
    // ====================================================

    /// <summary>Lookup RUC data using tenant's configured provider</summary>
    [HttpGet("lookup/ruc/{number}")]
    public async Task<IActionResult> LookupRuc(string number, CancellationToken ct)
    {
        if (number.Length != 11)
            return BadRequest(new { error = "RUC debe tener 11 dígitos" });

        var (config, error) = await GetConfigOrError(ct);
        if (error is not null) return error;
        if (config!.LookupProvider == "none" || string.IsNullOrEmpty(config.LookupApiKey))
            return BadRequest(new { error = "No hay proveedor de datos configurado. Ve a Configuración → Servicios Externos." });

        try
        {
            var result = await CallLookupProvider(config.LookupProvider, config.LookupApiKey, "ruc", number, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lookup RUC failed for {Number} with provider {Provider}", number, config.LookupProvider);
            return StatusCode(502, new { error = $"Error consultando al proveedor: {ex.Message}" });
        }
    }

    /// <summary>Lookup DNI data using tenant's configured provider</summary>
    [HttpGet("lookup/dni/{number}")]
    public async Task<IActionResult> LookupDni(string number, CancellationToken ct)
    {
        if (number.Length != 8)
            return BadRequest(new { error = "DNI debe tener 8 dígitos" });

        var (config, error) = await GetConfigOrError(ct);
        if (error is not null) return error;
        if (config!.LookupProvider == "none" || string.IsNullOrEmpty(config.LookupApiKey))
            return BadRequest(new { error = "No hay proveedor de datos configurado. Ve a Configuración → Servicios Externos." });

        try
        {
            var result = await CallLookupProvider(config.LookupProvider, config.LookupApiKey, "dni", number, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lookup DNI failed for {Number} with provider {Provider}", number, config.LookupProvider);
            return StatusCode(502, new { error = $"Error consultando al proveedor: {ex.Message}" });
        }
    }

    // ====================================================
    // AI CHAT PROXY
    // ====================================================

    /// <summary>Proxy chat message to tenant's configured AI provider</summary>
    [HttpPost("ai/chat")]
    public async Task<IActionResult> AiChat([FromBody] AiChatRequest request, CancellationToken ct)
    {
        var (config, error) = await GetConfigOrError(ct);
        if (error is not null) return error;
        if (config!.AiProvider == "none" || string.IsNullOrEmpty(config.AiApiKey))
            return BadRequest(new { error = "No hay proveedor de IA configurado. Ve a Configuración → Servicios Externos." });

        try
        {
            var response = await CallAiProvider(config.AiProvider, config.AiApiKey, config.AiModel, request.Message, ct);
            return Ok(new { response, provider = config.AiProvider, model = config.AiModel });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI chat failed with provider {Provider}", config.AiProvider);
            return StatusCode(502, new { error = $"Error del proveedor de IA: {ex.Message}" });
        }
    }

    /// <summary>Test AI provider key by sending a simple prompt and return status per model</summary>
    [HttpPost("ai/test")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> TestAiKey(CancellationToken ct)
    {
        var (config, error) = await GetConfigOrError(ct);
        if (error is not null) return error;
        if (config!.AiProvider == "none" || string.IsNullOrEmpty(config.AiApiKey))
            return BadRequest(new { error = "No hay proveedor de IA configurado." });

        // Get all models for this provider
        var allModels = config.AiProvider switch
        {
            "gemini" => new[] { "gemini-3.1-pro-preview", "gemini-3-flash", "gemini-2.5-pro", "gemini-2.5-flash" },
            "claude" => new[] { "claude-sonnet-4-6", "claude-opus-4-6", "claude-sonnet-4-5-20250929", "claude-haiku-4-5-20251001" },
            "grok" => new[] { "grok-4.20", "grok-4.1", "grok-3", "grok-3-mini" },
            "deepseek" => new[] { "deepseek-chat", "deepseek-reasoner" },
            "openai" => new[] { "gpt-5.3-codex", "gpt-5.2", "gpt-5", "gpt-5-mini", "gpt-4.1", "gpt-4.1-mini", "o3", "o3-mini" },
            _ => Array.Empty<string>()
        };

        var results = new List<object>();
        foreach (var model in allModels)
        {
            try
            {
                var response = await CallAiProvider(config.AiProvider, config.AiApiKey, model, "Responde solo: OK", ct);
                results.Add(new { model, status = "active", response = response[..Math.Min(50, response.Length)] });
            }
            catch (Exception ex)
            {
                results.Add(new { model, status = "error", response = ex.Message[..Math.Min(100, ex.Message.Length)] });
            }
        }

        return Ok(new { provider = config.AiProvider, models = results });
    }

    /// <summary>Get current lookup config status</summary>
    [HttpGet("lookup/status")]
    public async Task<IActionResult> LookupStatus(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var config = await _db.TenantServiceConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

        if (config is null || config.LookupProvider == "none" || string.IsNullOrEmpty(config.LookupApiKey))
            return Ok(new { configured = false, provider = "none" });

        var providerNames = new Dictionary<string, string>
        {
            ["apiperu"] = "ApiPeru.dev",
            ["migo"] = "Migo API",
            ["peruapi"] = "Perú API",
            ["apis_net"] = "APIs.net.pe",
        };
        var name = providerNames.GetValueOrDefault(config.LookupProvider, config.LookupProvider);

        return Ok(new { configured = true, provider = config.LookupProvider, providerName = name });
    }

    /// <summary>Get current AI config status for the floating chat widget</summary>
    [HttpGet("ai/status")]
    public async Task<IActionResult> AiStatus(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var config = await _db.TenantServiceConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

        if (config is null || config.AiProvider == "none" || string.IsNullOrEmpty(config.AiApiKey))
            return Ok(new { configured = false, provider = "none", model = (string?)null });

        return Ok(new { configured = true, provider = config.AiProvider, model = config.AiModel });
    }

    // ====================================================
    // PRIVATE: Provider Implementations
    // ====================================================

    private async Task<(TenantServiceConfig? config, IActionResult? error)> GetConfigOrError(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var config = await _db.TenantServiceConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

        if (config is null)
            return (null, BadRequest(new { error = "Servicios externos no configurados. Ve a Configuración → Servicios Externos." }));

        return (config, null);
    }

    private async Task<object> CallLookupProvider(string provider, string apiKey, string docType, string number, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);

        string url;
        switch (provider)
        {
            case "apiperu":
                url = docType == "ruc"
                    ? $"https://apiperu.dev/api/ruc/{number}"
                    : $"https://apiperu.dev/api/dni/{number}";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                break;

            case "migo":
                url = docType == "ruc"
                    ? $"https://api.migo.pe/api/v1/ruc?token={apiKey}&ruc={number}"
                    : $"https://api.migo.pe/api/v1/dni?token={apiKey}&dni={number}";
                break;

            case "peruapi":
                url = docType == "ruc"
                    ? $"https://api.peruapi.com/ruc/{number}"
                    : $"https://api.peruapi.com/dni/{number}";
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                break;

            case "apis_net":
                url = docType == "ruc"
                    ? $"https://api.apis.net.pe/v2/sunat/ruc?numero={number}"
                    : $"https://api.apis.net.pe/v2/reniec/dni?numero={number}";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Referrer = new Uri("https://apis.net.pe");
                break;

            default:
                throw new InvalidOperationException($"Proveedor desconocido: {provider}");
        }

        var response = await client.GetAsync(url, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Provider returned {response.StatusCode}: {content[..Math.Min(200, content.Length)]}");

        var json = JsonSerializer.Deserialize<JsonElement>(content);

        // Normalize response to a common format
        return NormalizeLookupResponse(provider, docType, json);
    }

    private static object NormalizeLookupResponse(string provider, string docType, JsonElement json)
    {
        // Normalize to a common shape regardless of provider
        if (docType == "ruc")
        {
            // apis.net.pe returns: nombre, razonSocial, numeroDocumento, estado, condicion, direccion, departamento, provincia, distrito
            // apiperu.dev wraps in "data" object with: nombre_o_razon_social, ruc, estado, condicion, direccion
            // migo returns: nombre, ruc, estado, condicion, direccion
            return new
            {
                type = "ruc",
                number = GetJsonString(json, "numeroDocumento", "ruc", "numero", "numRuc"),
                name = GetJsonString(json, "razonSocial", "nombre", "nombre_o_razon_social", "razon_social", "name"),
                tradeName = GetJsonString(json, "nombre_comercial", "nombreComercial"),
                address = GetJsonString(json, "direccion", "direccion_completa", "domicilioFiscal", "address"),
                status = GetJsonString(json, "estado", "estado_contribuyente", "status"),
                condition = GetJsonString(json, "condicion", "condicion_contribuyente", "condition"),
                department = GetJsonString(json, "departamento", "department"),
                province = GetJsonString(json, "provincia", "province"),
                district = GetJsonString(json, "distrito", "district"),
            };
        }
        else
        {
            // apis.net.pe returns: nombre (full), nombres, apellidoPaterno, apellidoMaterno, numeroDocumento
            // apiperu.dev wraps in "data" with: nombres, apellido_paterno, apellido_materno
            var fullName = GetJsonString(json, "nombre_completo", "nombreCompleto", "fullName");
            if (fullName == null)
            {
                // Build full name from parts
                var nombres = GetJsonString(json, "nombres", "nombre", "firstName", "names");
                var paterno = GetJsonString(json, "apellidoPaterno", "apellido_paterno", "paterno");
                var materno = GetJsonString(json, "apellidoMaterno", "apellido_materno", "materno");
                // apis.net.pe puts full name in "nombre" field
                if (paterno != null)
                    fullName = $"{nombres} {paterno} {materno}".Trim();
                else
                    fullName = nombres;
            }
            return new
            {
                type = "dni",
                number = GetJsonString(json, "numeroDocumento", "dni", "numero", "numDoc"),
                firstName = GetJsonString(json, "nombres", "nombre", "firstName", "names"),
                lastName = GetJsonString(json, "apellidoPaterno", "apellido_paterno", "paterno"),
                motherLastName = GetJsonString(json, "apellidoMaterno", "apellido_materno", "materno"),
                fullName,
            };
        }
    }

    private static string? GetJsonString(JsonElement el, params string[] keys)
    {
        // Try to find value in nested "data" object first, then root
        if (el.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            foreach (var key in keys)
                if (data.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.String)
                    return val.GetString();
        }

        foreach (var key in keys)
            if (el.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.String)
                return val.GetString();

        return null;
    }

    private async Task<string> CallAiProvider(string provider, string apiKey, string? model, string message, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        var systemPrompt = "Eres un asistente experto en facturación electrónica peruana (SUNAT). " +
            "Ayudas con temas de IGV, tipos de comprobante, series, notas de crédito, " +
            "Catálogos SUNAT, y regulaciones tributarias del Perú. Responde en español de forma concisa.";

        string url;
        object body;
        string? authHeader = null;

        switch (provider)
        {
            case "gemini":
                model ??= "gemini-3-flash";
                url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                body = new
                {
                    contents = new[] { new { parts = new[] { new { text = $"{systemPrompt}\n\nUsuario: {message}" } } } }
                };
                break;

            case "claude":
                model ??= "claude-sonnet-4-6";
                url = "https://api.anthropic.com/v1/messages";
                authHeader = apiKey;
                body = new
                {
                    model,
                    max_tokens = 1024,
                    system = systemPrompt,
                    messages = new[] { new { role = "user", content = message } }
                };
                break;

            case "grok":
                model ??= "grok-4.20";
                url = "https://api.x.ai/v1/chat/completions";
                authHeader = apiKey;
                body = new
                {
                    model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = message }
                    }
                };
                break;

            case "deepseek":
                model ??= "deepseek-chat";
                url = "https://api.deepseek.com/chat/completions";
                authHeader = apiKey;
                body = new
                {
                    model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = message }
                    }
                };
                break;

            case "openai":
                model ??= "gpt-5-mini";
                url = "https://api.openai.com/v1/chat/completions";
                authHeader = apiKey;
                body = new
                {
                    model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = message }
                    }
                };
                break;

            default:
                throw new InvalidOperationException($"AI provider desconocido: {provider}");
        }

        var jsonBody = JsonSerializer.Serialize(body);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };

        if (provider == "claude")
        {
            httpRequest.Headers.Add("x-api-key", authHeader);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        }
        else if (authHeader is not null)
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authHeader);
        }

        var response = await client.SendAsync(httpRequest, ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"AI provider returned {response.StatusCode}: {content[..Math.Min(300, content.Length)]}");

        var json = JsonSerializer.Deserialize<JsonElement>(content);

        // Extract text from different provider response formats
        return ExtractAiResponse(provider, json);
    }

    private static string ExtractAiResponse(string provider, JsonElement json)
    {
        try
        {
            return provider switch
            {
                "gemini" => json.GetProperty("candidates")[0]
                    .GetProperty("content").GetProperty("parts")[0]
                    .GetProperty("text").GetString() ?? "",

                "claude" => json.GetProperty("content")[0]
                    .GetProperty("text").GetString() ?? "",

                // OpenAI-compatible format (grok, deepseek, openai)
                _ => json.GetProperty("choices")[0]
                    .GetProperty("message").GetProperty("content").GetString() ?? ""
            };
        }
        catch
        {
            return json.GetRawText()[..Math.Min(500, json.GetRawText().Length)];
        }
    }

    // ====================================================
    // DTOs
    // ====================================================

    public record UpdateServiceConfigRequest(
        string? LookupProvider,
        string? LookupApiKey,
        string? AiProvider,
        string? AiApiKey,
        string? AiModel
    );

    public record AiChatRequest(string Message);
}
