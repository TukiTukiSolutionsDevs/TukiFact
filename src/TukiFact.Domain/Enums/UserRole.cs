namespace TukiFact.Domain.Enums;

public static class UserRole
{
    public const string Admin = "admin";
    public const string Emisor = "emisor";
    public const string Consulta = "consulta";

    public static readonly string[] All = [Admin, Emisor, Consulta];

    public static bool IsValid(string role) => All.Contains(role);
}
