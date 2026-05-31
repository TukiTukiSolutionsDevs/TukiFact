using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TukiFact.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Subscriptions_AddTableWithCulqiFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_plans_PlanId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_tenants_TenantId",
                table: "Subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Subscriptions",
                table: "Subscriptions");

            migrationBuilder.RenameTable(
                name: "Subscriptions",
                newName: "subscriptions");

            migrationBuilder.RenameIndex(
                name: "IX_Subscriptions_TenantId",
                table: "subscriptions",
                newName: "IX_subscriptions_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Subscriptions_PlanId",
                table: "subscriptions",
                newName: "IX_subscriptions_PlanId");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "subscriptions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "active",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartDate",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlyAmount",
                table: "subscriptions",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "DocumentsUsedThisMonth",
                table: "subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "subscriptions",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "subscriptions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CulqiCardId",
                table: "subscriptions",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CulqiCustomerId",
                table: "subscriptions",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CulqiSubscriptionId",
                table: "subscriptions",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastChargeId",
                table: "subscriptions",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastChargedAt",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CulqiPlanId",
                table: "plans",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_subscriptions",
                table: "subscriptions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_CulqiSubscriptionId",
                table: "subscriptions",
                column: "CulqiSubscriptionId",
                unique: true,
                filter: "\"CulqiSubscriptionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_Status_NextBillingDate",
                table: "subscriptions",
                columns: new[] { "Status", "NextBillingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_plans_CulqiPlanId",
                table: "plans",
                column: "CulqiPlanId",
                unique: true,
                filter: "\"CulqiPlanId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_subscriptions_plans_PlanId",
                table: "subscriptions",
                column: "PlanId",
                principalTable: "plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_subscriptions_tenants_TenantId",
                table: "subscriptions",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_subscriptions_plans_PlanId",
                table: "subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_subscriptions_tenants_TenantId",
                table: "subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_subscriptions",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_subscriptions_CulqiSubscriptionId",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_subscriptions_Status_NextBillingDate",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_plans_CulqiPlanId",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "CulqiCardId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "CulqiCustomerId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "CulqiSubscriptionId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "LastChargeId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "LastChargedAt",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "CulqiPlanId",
                table: "plans");

            migrationBuilder.RenameTable(
                name: "subscriptions",
                newName: "Subscriptions");

            migrationBuilder.RenameIndex(
                name: "IX_subscriptions_TenantId",
                table: "Subscriptions",
                newName: "IX_Subscriptions_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_subscriptions_PlanId",
                table: "Subscriptions",
                newName: "IX_Subscriptions_PlanId");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Subscriptions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "active");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartDate",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<decimal>(
                name: "MonthlyAmount",
                table: "Subscriptions",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "DocumentsUsedThisMonth",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Subscriptions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Subscriptions",
                table: "Subscriptions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_plans_PlanId",
                table: "Subscriptions",
                column: "PlanId",
                principalTable: "plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_tenants_TenantId",
                table: "Subscriptions",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
