# 04 — ICBPER (Impuesto al Consumo de Bolsas de Plástico)

> **Prioridad**: 🟡 MEDIA — Obligatorio para establecimientos que venden bolsas.
> **Normativa**: Ley 30884, art. 12
> **Fuente**: gob.pe, orientacion.sunat.gob.pe

---

## Regla

- **S/ 0.50 por bolsa** desde 2023 en adelante (Ley 30884 art. 12)
- Aplica a bolsas de plástico ofrecidas en tiendas/servicios afectos al IGV
- **NO forma parte de la base imponible del IGV** — se suma al total aparte
- Código tributo SUNAT: **7152** (tipo internacional: OTH)
- El establecimiento es agente de percepción del impuesto

## Cronograma histórico

| Año | Monto por bolsa |
|:---:|:---------------:|
| 2019 | S/ 0.10 |
| 2020 | S/ 0.20 |
| 2021 | S/ 0.30 |
| 2022 | S/ 0.40 |
| 2023+ | S/ 0.50 |

## XML UBL 2.1

### En el Item (TaxTotal del ítem)

```xml
<cac:TaxTotal>
  <cbc:TaxAmount currencyID="PEN">2.50</cbc:TaxAmount>
  <cac:TaxSubtotal>
    <cbc:TaxableAmount currencyID="PEN">5</cbc:TaxableAmount> <!-- cant. bolsas -->
    <cbc:TaxAmount currencyID="PEN">2.50</cbc:TaxAmount>
    <cac:TaxCategory>
      <cbc:ID schemeID="UN/ECE 5305">S</cbc:ID>
      <cbc:PerUnitAmount currencyID="PEN">0.50</cbc:PerUnitAmount>
      <cbc:BaseUnitMeasure unitCode="NIU">5</cbc:BaseUnitMeasure>
      <cac:TaxScheme>
        <cbc:ID>7152</cbc:ID>
        <cbc:Name>ICBPER</cbc:Name>
        <cbc:TaxTypeCode>OTH</cbc:TaxTypeCode>
      </cac:TaxScheme>
    </cac:TaxCategory>
  </cac:TaxSubtotal>
</cac:TaxTotal>
```

### En totales globales

```xml
<!-- Sumar ICBPER en LegalMonetaryTotal -->
<cac:LegalMonetaryTotal>
  <cbc:ChargeTotalAmount currencyID="PEN">2.50</cbc:ChargeTotalAmount>
  <!-- PayableAmount incluye ICBPER -->
  <cbc:PayableAmount currencyID="PEN">120.50</cbc:PayableAmount>
</cac:LegalMonetaryTotal>
```

## Implementación

### Cambios en DocumentItem

```csharp
public int IcbperBagQuantity { get; set; } // cantidad de bolsas
public decimal IcbperUnitAmount { get; set; } = 0.50m; // monto por bolsa
public decimal IcbperTotal => IcbperBagQuantity * IcbperUnitAmount;
```

### Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `Domain/Entities/DocumentItem.cs` | Agregar campos ICBPER |
| `Infrastructure/Services/XmlBuilders/*.cs` | Agregar TaxSubtotal 7152 |
| `Infrastructure/Services/PdfGenerator.cs` | Mostrar ICBPER desglosado |
| Frontend emisión | Campo "Cantidad de bolsas" por ítem |
| `DocumentService.cs` | Incluir ICBPER en cálculo de totales |
