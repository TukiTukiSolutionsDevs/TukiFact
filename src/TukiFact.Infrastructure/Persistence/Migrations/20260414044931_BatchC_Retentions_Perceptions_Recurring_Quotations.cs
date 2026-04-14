using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TukiFact.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BatchC_Retentions_Perceptions_Recurring_Quotations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "documents",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExchangeRateDate",
                table: "documents",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "perception_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Serie = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Correlative = table.Column<long>(type: "bigint", nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerDocType = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    CustomerDocNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    RegimeCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    PerceptionPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TotalInvoiceAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    TotalPerceived = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    TotalCollected = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "PEN"),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    SunatResponseCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SunatResponseDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HashCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    XmlUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PdfUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CdrUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perception_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_perception_documents_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Correlative = table.Column<long>(type: "bigint", nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerDocType = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    CustomerDocNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CustomerEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CustomerPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "PEN"),
                    Subtotal = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Igv = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    TotalDiscount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false, defaultValue: 0m),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    InvoiceDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceDocumentNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TermsAndConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PdfUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quotations_documents_InvoiceDocumentId",
                        column: x => x.InvoiceDocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_quotations_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recurring_invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Serie = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    CustomerDocType = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    CustomerDocNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CustomerEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ItemsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "PEN"),
                    Frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NextEmissionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "active"),
                    EmittedCount = table.Column<int>(type: "integer", nullable: false),
                    LastEmittedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recurring_invoices_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "retention_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Serie = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Correlative = table.Column<long>(type: "bigint", nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SupplierDocType = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    SupplierDocNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SupplierAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    RegimeCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    RetentionPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TotalInvoiceAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    TotalRetained = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    TotalPaid = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "PEN"),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    SunatResponseCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SunatResponseDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HashCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    XmlUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PdfUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CdrUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retention_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_retention_documents_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "perception_document_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PerceptionDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DocumentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InvoiceAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    InvoiceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "PEN"),
                    CollectionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CollectionNumber = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CollectionAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    PerceivedAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    TotalCollectedAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    ExchangeRateDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perception_document_references", x => x.Id);
                    table.ForeignKey(
                        name: "FK_perception_document_references_perception_documents_Percept~",
                        column: x => x.PerceptionDocumentId,
                        principalTable: "perception_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quotation_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    UnitMeasure = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "NIU"),
                    UnitPrice = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false, defaultValue: 0m),
                    IgvType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false, defaultValue: "10"),
                    IgvAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quotation_items_quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "retention_document_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    RetentionDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DocumentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InvoiceAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    InvoiceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "PEN"),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PaymentNumber = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    PaymentAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    RetainedAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    NetPaidAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    ExchangeRateDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retention_document_references", x => x.Id);
                    table.ForeignKey(
                        name: "FK_retention_document_references_retention_documents_Retention~",
                        column: x => x.RetentionDocumentId,
                        principalTable: "retention_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_perception_document_references_PerceptionDocumentId",
                table: "perception_document_references",
                column: "PerceptionDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_perception_documents_TenantId_IssueDate",
                table: "perception_documents",
                columns: new[] { "TenantId", "IssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_perception_documents_TenantId_Serie_Correlative",
                table: "perception_documents",
                columns: new[] { "TenantId", "Serie", "Correlative" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_perception_documents_TenantId_Status",
                table: "perception_documents",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_quotation_items_QuotationId",
                table: "quotation_items",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_InvoiceDocumentId",
                table: "quotations",
                column: "InvoiceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_TenantId_IssueDate",
                table: "quotations",
                columns: new[] { "TenantId", "IssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_quotations_TenantId_QuotationNumber",
                table: "quotations",
                columns: new[] { "TenantId", "QuotationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotations_TenantId_Status",
                table: "quotations",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_recurring_invoices_NextEmissionDate",
                table: "recurring_invoices",
                column: "NextEmissionDate");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_invoices_TenantId_Status",
                table: "recurring_invoices",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_retention_document_references_RetentionDocumentId",
                table: "retention_document_references",
                column: "RetentionDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_retention_documents_TenantId_IssueDate",
                table: "retention_documents",
                columns: new[] { "TenantId", "IssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_retention_documents_TenantId_Serie_Correlative",
                table: "retention_documents",
                columns: new[] { "TenantId", "Serie", "Correlative" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_retention_documents_TenantId_Status",
                table: "retention_documents",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "perception_document_references");

            migrationBuilder.DropTable(
                name: "quotation_items");

            migrationBuilder.DropTable(
                name: "recurring_invoices");

            migrationBuilder.DropTable(
                name: "retention_document_references");

            migrationBuilder.DropTable(
                name: "perception_documents");

            migrationBuilder.DropTable(
                name: "quotations");

            migrationBuilder.DropTable(
                name: "retention_documents");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "ExchangeRateDate",
                table: "documents");
        }
    }
}
