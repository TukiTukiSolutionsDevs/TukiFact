namespace TukiFact.Domain.Enums;

public static class DocumentType
{
    public const string Factura = "01";
    public const string Boleta = "03";
    public const string NotaCredito = "07";
    public const string NotaDebito = "08";

    public static readonly string[] All = [Factura, Boleta, NotaCredito, NotaDebito];
    public static bool IsValid(string type) => All.Contains(type);

    public static string GetName(string type) => type switch
    {
        "01" => "FACTURA ELECTRÓNICA",
        "03" => "BOLETA DE VENTA ELECTRÓNICA",
        "07" => "NOTA DE CRÉDITO ELECTRÓNICA",
        "08" => "NOTA DE DÉBITO ELECTRÓNICA",
        _ => "DESCONOCIDO"
    };
}
