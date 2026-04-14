namespace TukiFact.Domain.Entities;

public class Ubigeo
{
    public string Code { get; set; } = string.Empty; // "040601" (6 dígitos INEI)
    public string Department { get; set; } = string.Empty; // "AREQUIPA"
    public string Province { get; set; } = string.Empty; // "AREQUIPA"
    public string District { get; set; } = string.Empty; // "CAYMA"
    public bool IsActive { get; set; } = true;
}
