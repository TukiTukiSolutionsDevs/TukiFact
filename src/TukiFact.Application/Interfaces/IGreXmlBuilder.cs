using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

/// <summary>
/// Builds DespatchAdvice UBL 2.1 XML for GRE (Guía de Remisión Electrónica).
/// </summary>
public interface IGreXmlBuilder
{
    /// <summary>
    /// Build GRE Remitente XML (type 09, serie T###).
    /// </summary>
    string BuildDespatchAdviceXml(DespatchAdvice despatchAdvice, Tenant tenant);
}
