using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TukiFact.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint4_NC_ND_Void_Dashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreditNoteReason",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DebitNoteReason",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReferenceDocumentId",
                table: "documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceDocumentNumber",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceDocumentType",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "voided_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    TicketNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReferenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SunatTicket = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    SunatResponseCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SunatResponseDescription = table.Column<string>(type: "text", nullable: true),
                    ItemsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voided_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_voided_documents_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_documents_ReferenceDocumentId",
                table: "documents",
                column: "ReferenceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_voided_documents_TenantId_TicketNumber",
                table: "voided_documents",
                columns: new[] { "TenantId", "TicketNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_documents_documents_ReferenceDocumentId",
                table: "documents",
                column: "ReferenceDocumentId",
                principalTable: "documents",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_documents_ReferenceDocumentId",
                table: "documents");

            migrationBuilder.DropTable(
                name: "voided_documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_ReferenceDocumentId",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "CreditNoteReason",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "DebitNoteReason",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "ReferenceDocumentId",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "ReferenceDocumentNumber",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "ReferenceDocumentType",
                table: "documents");
        }
    }
}
