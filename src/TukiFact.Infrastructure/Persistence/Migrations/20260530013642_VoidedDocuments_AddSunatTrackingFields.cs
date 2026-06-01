using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TukiFact.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VoidedDocuments_AddSunatTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CdrUrl",
                table: "voided_documents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "voided_documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastPolledAt",
                table: "voided_documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "voided_documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "XmlUrl",
                table: "voided_documents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_voided_documents_Status_LastPolledAt",
                table: "voided_documents",
                columns: new[] { "Status", "LastPolledAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_voided_documents_Status_LastPolledAt",
                table: "voided_documents");

            migrationBuilder.DropColumn(
                name: "CdrUrl",
                table: "voided_documents");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "voided_documents");

            migrationBuilder.DropColumn(
                name: "LastPolledAt",
                table: "voided_documents");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "voided_documents");

            migrationBuilder.DropColumn(
                name: "XmlUrl",
                table: "voided_documents");
        }
    }
}
