namespace TukiFact.Domain.Enums;

/// <summary>
/// SUNAT Catálogo 10 - Códigos de tipo de nota de débito
/// </summary>
public static class DebitNoteReason
{
    public const string InteresesPorMora = "01";
    public const string AumentoEnElValor = "02";
    public const string PenalidadOtrosConceptos = "03";

    public static string GetDescription(string code) => code switch
    {
        "01" => "Intereses por mora",
        "02" => "Aumento en el valor",
        "03" => "Penalidades / otros conceptos",
        _ => "Otros"
    };
}
