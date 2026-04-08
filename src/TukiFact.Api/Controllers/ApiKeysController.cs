using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.ApiKeys;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Interfaces;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/api-keys")]
[Authorize(Roles = "admin")]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyRepository _apiKeyRepo;
    private readonly ITenantProvider _tenantProvider;

    public ApiKeysController(IApiKeyRepository apiKeyRepo, ITenantProvider tenantProvider)
    {
        _apiKeyRepo = apiKeyRepo;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var keys = await _apiKeyRepo.GetByTenantAsync(tenantId, ct);
        var response = keys.Select(k => new ApiKeyResponse(
            k.Id, k.KeyPrefix, k.Name,
            System.Text.Json.JsonSerializer.Deserialize<string[]>(k.Permissions) ?? [],
            k.IsActive, k.LastUsedAt, k.CreatedAt));
        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApiKeyRequest request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Generate a random API key
        var rawKey = $"tk_{Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant()}";
        var keyHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawKey))).ToLowerInvariant();
        var keyPrefix = rawKey[..11]; // "tk_" + 8 chars

        var apiKey = new ApiKey
        {
            TenantId = tenantId,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Name = request.Name,
            Permissions = System.Text.Json.JsonSerializer.Serialize(request.Permissions)
        };

        await _apiKeyRepo.CreateAsync(apiKey, ct);

        var response = new ApiKeyResponse(
            apiKey.Id, apiKey.KeyPrefix, apiKey.Name,
            request.Permissions, apiKey.IsActive, null, apiKey.CreatedAt,
            PlainTextKey: rawKey);

        return Created($"/v1/api-keys/{apiKey.Id}", response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        await _apiKeyRepo.RevokeAsync(id, ct);
        return NoContent();
    }
}
