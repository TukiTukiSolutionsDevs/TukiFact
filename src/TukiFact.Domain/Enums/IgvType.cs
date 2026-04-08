namespace TukiFact.Domain.Enums;

public static class IgvType
{
    public const string Gravado = "10";
    public const string Exonerado = "20";
    public const string Inafecto = "30";
    public const string Gratuito = "21";

    public static string GetSunatCode(string type) => type switch
    {
        "10" => "1000", // IGV - Impuesto General a las Ventas
        "20" => "9997", // EXO - Exonerado
        "30" => "9998", // INA - Inafecto
        "21" => "9996", // GRA - Gratuito
        _ => "1000"
    };

    public static string GetSunatName(string type) => type switch
    {
        "10" => "IGV",
        "20" => "EXO",
        "30" => "INA",
        "21" => "GRA",
        _ => "IGV"
    };
}
