namespace TukiFact.Application.DTOs.Auth;

public record LoginWithGoogleRequest(string IdToken, Guid? TenantId);
