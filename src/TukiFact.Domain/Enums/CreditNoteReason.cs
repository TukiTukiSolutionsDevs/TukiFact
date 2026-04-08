namespace TukiFact.Domain.Enums;

/// <summary>
/// SUNAT Catálogo 09 - Códigos de tipo de nota de crédito
/// </summary>
public static class CreditNoteReason
{
    public const string AnulacionOperacion = "01";
    public const string AnulacionPorErrorEnRuc = "02";
    public const string CorreccionPorErrorDescripcion = "03";
    public const string DescuentoGlobal = "04";
    public const string DescuentoPorItem = "05";
    public const string DevolucionTotal = "06";
    public const string DevolucionPorItem = "07";
    public const string Bonificacion = "08";
    public const string DisminucionValor = "09";
    public const string OtrosConceptos = "10";

    public static string GetDescription(string code) => code switch
    {
        "01" => "Anulación de la operación",
        "02" => "Anulación por error en el RUC",
        "03" => "Corrección por error en la descripción",
        "04" => "Descuento global",
        "05" => "Descuento por ítem",
        "06" => "Devolución total",
        "07" => "Devolución por ítem",
        "08" => "Bonificación",
        "09" => "Disminución en el valor",
        "10" => "Otros conceptos",
        _ => "Otros"
    };
}
