using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly string _clientId;

    public GoogleAuthService(IConfiguration configuration)
    {
        _clientId = configuration["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId is not configured");
    }

    public async Task<GoogleUserInfo> ValidateIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _clientId }
                });

            if (!payload.EmailVerified)
                throw new UnauthorizedAccessException("Google email not verified");

            return new GoogleUserInfo(
                Email: payload.Email,
                Name: payload.Name,
                Picture: payload.Picture,
                Subject: payload.Subject);
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedAccessException("Token de Google inválido");
        }
        catch (FormatException)
        {
            // Malformed base64 inside the JWT segments — same user-facing error.
            throw new UnauthorizedAccessException("Token de Google inválido");
        }
        catch (ArgumentException)
        {
            // Empty / null id_token.
            throw new UnauthorizedAccessException("Token de Google inválido");
        }
    }
}
