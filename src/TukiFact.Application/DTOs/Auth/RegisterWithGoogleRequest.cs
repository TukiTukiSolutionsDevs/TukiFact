namespace TukiFact.Application.DTOs.Auth;

public record RegisterWithGoogleRequest(
    string IdToken,
    string Ruc,
    string RazonSocial,
    string? NombreComercial,
    string? Direccion);
