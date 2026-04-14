using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TukiFact.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BatchA_GRE_Email_ResetPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GreClientId",
                table: "tenants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GreClientSecret",
                table: "tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoSendEmail",
                table: "tenant_service_configs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EmailFromAddress",
                table: "tenant_service_configs",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailFromName",
                table: "tenant_service_configs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailProvider",
                table: "tenant_service_configs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "log");

            migrationBuilder.AddColumn<string>(
                name: "ResendApiKey",
                table: "tenant_service_configs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpHost",
                table: "tenant_service_configs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpPassword",
                table: "tenant_service_configs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmtpPort",
                table: "tenant_service_configs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpUser",
                table: "tenant_service_configs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // NOTE: customers table already exists from previous migration — skipped

            migrationBuilder.CreateTable(
                name: "despatch_advices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Serie = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Correlative = table.Column<long>(type: "bigint", nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IssueTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    TransferStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TransferReasonCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    TransferReasonDescription = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GrossWeight = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    WeightUnitCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "KGM"),
                    TotalPackages = table.Column<int>(type: "integer", nullable: false),
                    TransportMode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    CarrierDocType = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    CarrierDocNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    CarrierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CarrierMtcNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DriverDocType = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    DriverDocNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    DriverName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DriverLicense = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    VehiclePlate = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SecondaryVehiclePlate = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    RecipientDocType = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    RecipientDocNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OriginUbigeo = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    OriginAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DestinationUbigeo = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    DestinationAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RelatedDocType = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    RelatedDocNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    SunatResponseCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SunatResponseMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SunatTicket = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    XmlUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PdfUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CdrUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_despatch_advices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_despatch_advices_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "email_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    To = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Cc = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Template = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "generic"),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "log"),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_logs_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_password_reset_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // NOTE: platform_users table already exists from previous migration — skipped
            // NOTE: products table already exists from previous migration — skipped

            migrationBuilder.CreateTable(
                name: "ubigeo",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Department = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Province = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    District = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ubigeo", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "despatch_advice_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    DespatchAdviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProductCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    UnitCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "NIU")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_despatch_advice_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_despatch_advice_items_despatch_advices_DespatchAdviceId",
                        column: x => x.DespatchAdviceId,
                        principalTable: "despatch_advices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // NOTE: IX_customers_TenantId_DocNumber already exists — skipped

            migrationBuilder.CreateIndex(
                name: "IX_despatch_advice_items_DespatchAdviceId",
                table: "despatch_advice_items",
                column: "DespatchAdviceId");

            migrationBuilder.CreateIndex(
                name: "IX_despatch_advices_TenantId_IssueDate",
                table: "despatch_advices",
                columns: new[] { "TenantId", "IssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_despatch_advices_TenantId_Serie_Correlative",
                table: "despatch_advices",
                columns: new[] { "TenantId", "Serie", "Correlative" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_despatch_advices_TenantId_Status",
                table: "despatch_advices",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_email_logs_DocumentId",
                table: "email_logs",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_email_logs_TenantId_CreatedAt",
                table: "email_logs",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_email_logs_TenantId_Status",
                table: "email_logs",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_Token",
                table: "password_reset_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_UserId",
                table: "password_reset_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ubigeo_Department",
                table: "ubigeo",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_ubigeo_Department_Province",
                table: "ubigeo",
                columns: new[] { "Department", "Province" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // NOTE: customers table not created by this migration — skip drop

            migrationBuilder.DropTable(
                name: "despatch_advice_items");

            migrationBuilder.DropTable(
                name: "email_logs");

            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            // NOTE: customers, platform_users, products not created by this migration — skip drop

            migrationBuilder.DropTable(
                name: "ubigeo");

            migrationBuilder.DropTable(
                name: "despatch_advices");

            migrationBuilder.DropColumn(
                name: "GreClientId",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "GreClientSecret",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "AutoSendEmail",
                table: "tenant_service_configs");

            migrationBuilder.DropColumn(
                name: "EmailFromAddress",
                table: "tenant_service_configs");

            migrationBuilder.DropColumn(
                name: "EmailFromName",
                table: "tenant_service_configs");

            migrationBuilder.DropColumn(
                name: "EmailProvider",
                table: "tenant_service_configs");

            migrationBuilder.DropColumn(
                name: "ResendApiKey",
                table: "tenant_service_configs");

            migrationBuilder.DropColumn(
                name: "SmtpHost",
                table: "tenant_service_configs");

            migrationBuilder.DropColumn(
                name: "SmtpPassword",
                table: "tenant_service_configs");

            migrationBuilder.DropColumn(
                name: "SmtpPort",
                table: "tenant_service_configs");

            migrationBuilder.DropColumn(
                name: "SmtpUser",
                table: "tenant_service_configs");
        }
    }
}
