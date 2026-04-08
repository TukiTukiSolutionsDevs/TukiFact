namespace TukiFact.Application.DTOs.Auth;

public record RegisterRequest(
    string Ruc,
    string RazonSocial,
    string? NombreComercial,
    string? Direccion,
    string AdminEmail,
    string AdminPassword,
    string AdminFullName
);
