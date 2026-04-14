using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TukiFact.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BatchB_ExchangeRate_Detraction_Catalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DetractionAmount",
                table: "documents",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetractionBankAccount",
                table: "documents",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetractionCode",
                table: "documents",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DetractionPercent",
                table: "documents",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasDetraction",
                table: "documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "IcbperTotal",
                table: "documents",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "IcbperBagQuantity",
                table: "document_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "IcbperUnitAmount",
                table: "document_items",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0.50m);

            migrationBuilder.CreateTable(
                name: "detraction_codes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Annex = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detraction_codes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "exchange_rates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    BuyRate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    SellRate = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "SBS"),
                    FetchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exchange_rates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sunat_catalogs",
                columns: table => new
                {
                    CatalogNumber = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sunat_catalogs", x => x.CatalogNumber);
                });

            migrationBuilder.CreateTable(
                name: "sunat_catalog_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CatalogNumber = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sunat_catalog_codes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sunat_catalog_codes_sunat_catalogs_CatalogNumber",
                        column: x => x.CatalogNumber,
                        principalTable: "sunat_catalogs",
                        principalColumn: "CatalogNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_detraction_codes_Annex",
                table: "detraction_codes",
                column: "Annex");

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rates_Date_Currency",
                table: "exchange_rates",
                columns: new[] { "Date", "Currency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sunat_catalog_codes_CatalogNumber_Code",
                table: "sunat_catalog_codes",
                columns: new[] { "CatalogNumber", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "detraction_codes");

            migrationBuilder.DropTable(
                name: "exchange_rates");

            migrationBuilder.DropTable(
                name: "sunat_catalog_codes");

            migrationBuilder.DropTable(
                name: "sunat_catalogs");

            migrationBuilder.DropColumn(
                name: "DetractionAmount",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "DetractionBankAccount",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "DetractionCode",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "DetractionPercent",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "HasDetraction",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "IcbperTotal",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "IcbperBagQuantity",
                table: "document_items");

            migrationBuilder.DropColumn(
                name: "IcbperUnitAmount",
                table: "document_items");
        }
    }
}
