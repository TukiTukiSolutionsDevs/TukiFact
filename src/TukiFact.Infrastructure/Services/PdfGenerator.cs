using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Enums;
using DomainDocument = TukiFact.Domain.Entities.Document;
using DomainTenant = TukiFact.Domain.Entities.Tenant;

namespace TukiFact.Infrastructure.Services;

public class PdfGenerator : IPdfGenerator
{
    static PdfGenerator()
    {
        // QuestPDF community license — free for open source / indie use
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(DomainDocument document, DomainTenant tenant)
    {
        var docTypeName = DocumentType.GetName(document.DocumentType);

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });

                void ComposeHeader(IContainer c)
                {
                    c.Row(row =>
                    {
                        // Company info
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(tenant.RazonSocial).Bold().FontSize(14);
                            col.Item().Text($"RUC: {tenant.Ruc}");
                            if (tenant.Direccion is not null)
                                col.Item().Text(tenant.Direccion);
                        });

                        // Document box
                        row.ConstantItem(200).Border(1).Padding(8).Column(col =>
                        {
                            col.Item().AlignCenter().Text(docTypeName).Bold().FontSize(12);
                            col.Item().AlignCenter().Text(document.FullNumber).Bold().FontSize(12);
                        });
                    });
                }

                void ComposeContent(IContainer c)
                {
                    c.Column(col =>
                    {
                        col.Spacing(10);

                        // Issue date row
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Fecha de emisión: {document.IssueDate:dd/MM/yyyy}");
                            if (document.DueDate.HasValue)
                                row.RelativeItem().AlignRight().Text($"Fecha de vencimiento: {document.DueDate:dd/MM/yyyy}");
                        });

                        // Customer info
                        col.Item().Border(1).Padding(8).Column(info =>
                        {
                            info.Item().Text("DATOS DEL CLIENTE").Bold();
                            info.Item().Text($"Razón Social: {document.CustomerName}");
                            info.Item().Text($"Doc. Identidad: {document.CustomerDocType} - {document.CustomerDocNumber}");
                            if (document.CustomerAddress is not null)
                                info.Item().Text($"Dirección: {document.CustomerAddress}");
                        });

                        // Reference document (NC/ND)
                        if (document.ReferenceDocumentNumber is not null)
                        {
                            col.Item().Background(Colors.Yellow.Lighten3).Padding(6)
                                .Text($"Documento de referencia: {document.ReferenceDocumentNumber}");
                        }

                        // Items table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(35);   // #
                                columns.RelativeColumn(3);     // Description
                                columns.RelativeColumn();      // Qty
                                columns.RelativeColumn();      // Unit
                                columns.RelativeColumn();      // Unit price
                                columns.RelativeColumn();      // IGV
                                columns.RelativeColumn();      // Total
                            });

                            table.Header(header =>
                            {
                                IContainer CellHeader(IContainer container) =>
                                    container.Background(Colors.Grey.Darken2).Padding(5)
                                        .DefaultTextStyle(x => x.FontColor(Colors.White).Bold());

                                header.Cell().Element(CellHeader).Text("#");
                                header.Cell().Element(CellHeader).Text("Descripción");
                                header.Cell().Element(CellHeader).AlignRight().Text("Cant.");
                                header.Cell().Element(CellHeader).Text("U.M.");
                                header.Cell().Element(CellHeader).AlignRight().Text("P. Unit.");
                                header.Cell().Element(CellHeader).AlignRight().Text("IGV");
                                header.Cell().Element(CellHeader).AlignRight().Text("Total");
                            });

                            IContainer CellBody(IContainer container) =>
                                container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

                            foreach (var item in document.Items.OrderBy(i => i.Sequence))
                            {
                                table.Cell().Element(CellBody).Text(item.Sequence.ToString());
                                table.Cell().Element(CellBody).Text(item.Description);
                                table.Cell().Element(CellBody).AlignRight().Text(item.Quantity.ToString("F2"));
                                table.Cell().Element(CellBody).Text(item.UnitMeasure);
                                table.Cell().Element(CellBody).AlignRight().Text(item.UnitPrice.ToString("F2"));
                                table.Cell().Element(CellBody).AlignRight().Text(item.IgvAmount.ToString("F2"));
                                table.Cell().Element(CellBody).AlignRight().Text(item.Total.ToString("F2"));
                            }
                        });

                        // Totals
                        col.Item().AlignRight().Width(250).Column(totals =>
                        {
                            if (document.OperacionGravada > 0)
                                totals.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Op. Gravadas:");
                                    r.ConstantItem(100).AlignRight().Text($"{document.OperacionGravada:F2}");
                                });
                            if (document.OperacionExonerada > 0)
                                totals.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Op. Exoneradas:");
                                    r.ConstantItem(100).AlignRight().Text($"{document.OperacionExonerada:F2}");
                                });
                            if (document.OperacionInafecta > 0)
                                totals.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Op. Inafectas:");
                                    r.ConstantItem(100).AlignRight().Text($"{document.OperacionInafecta:F2}");
                                });

                            totals.Item().Row(r =>
                            {
                                r.RelativeItem().Text("IGV (18%):");
                                r.ConstantItem(100).AlignRight().Text($"{document.Igv:F2}");
                            });

                            totals.Item().BorderTop(1).Row(r =>
                            {
                                r.RelativeItem().Text("TOTAL:").Bold();
                                r.ConstantItem(100).AlignRight()
                                    .Text($"{document.Currency} {document.Total:F2}").Bold();
                            });
                        });

                        // Hash
                        if (document.HashCode is not null)
                        {
                            col.Item().Text($"Hash: {document.HashCode}")
                                .FontSize(7).FontColor(Colors.Grey.Medium);
                        }

                        // Notes
                        if (document.Notes is not null)
                        {
                            col.Item().Text($"Notas: {document.Notes}").Italic();
                        }
                    });
                }
            });
        }).GeneratePdf();
    }
}
