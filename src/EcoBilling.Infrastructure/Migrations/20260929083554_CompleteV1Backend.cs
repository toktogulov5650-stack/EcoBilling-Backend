ï»¿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteV1Backend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "idempotency_key",
                schema: "payments",
                table: "payments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "meters",
                table: "meters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "replaces_meter_id",
                schema: "meters",
                table: "meters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "retired_at",
                schema: "meters",
                table: "meters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "value",
                schema: "readings",
                table: "meter_readings",
                type: "numeric(18,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<Guid>(
                name: "author_user_id",
                schema: "readings",
                table: "meter_readings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "correction_reason",
                schema: "readings",
                table: "meter_readings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "source",
                schema: "readings",
                table: "meter_readings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "supersedes_reading_id",
                schema: "readings",
                table: "meter_readings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                schema: "billing",
                table: "charges",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "calculation_version",
                schema: "billing",
                table: "charges",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "consumption",
                schema: "billing",
                table: "charges",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "currency",
                schema: "billing",
                table: "charges",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "current_reading_id",
                schema: "billing",
                table: "charges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "previous_reading_id",
                schema: "billing",
                table: "charges",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overpayment",
                schema: "accounts",
                table: "accounts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "account_tariff_assignments",
                schema: "tariffs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tariff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_tariff_assignments", x => x.id);
                    table.CheckConstraint("ck_account_tariff_assignments_effective_period", "effective_to IS NULL OR effective_to > effective_from");
                    table.ForeignKey(
                        name: "fk_account_tariff_assignments_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "accounts",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_account_tariff_assignments_tariffs_tariff_id",
                        column: x => x.tariff_id,
                        principalSchema: "tariffs",
                        principalTable: "tariffs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "controller_assignments",
                schema: "controllers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    controller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_controller_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_controller_assignments_addresses_address_id",
                        column: x => x.address_id,
                        principalSchema: "accounts",
                        principalTable: "addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_controller_assignments_controllers_controller_id",
                        column: x => x.controller_id,
                        principalSchema: "controllers",
                        principalTable: "controllers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    charge_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_allocations", x => x.id);
                    table.CheckConstraint("ck_payment_allocations_amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "fk_payment_allocations_charges_charge_id",
                        column: x => x.charge_id,
                        principalSchema: "billing",
                        principalTable: "charges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_allocations_payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "payments",
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_tariffs_name",
                schema: "tariffs",
                table: "tariffs",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_meters_replaces_meter_id",
                schema: "meters",
                table: "meters",
                column: "replaces_meter_id",
                unique: true,
                filter: "replaces_meter_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_meters_serial_number",
                schema: "meters",
                table: "meters",
                column: "serial_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_meter_readings_author_user_id",
                schema: "readings",
                table: "meter_readings",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_meter_readings_supersedes_reading_id",
                schema: "readings",
                table: "meter_readings",
                column: "supersedes_reading_id",
                unique: true,
                filter: "supersedes_reading_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_meter_readings_source",
                schema: "readings",
                table: "meter_readings",
                sql: "source IN (1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "ix_charges_current_reading_id",
                schema: "billing",
                table: "charges",
                column: "current_reading_id");

            migrationBuilder.CreateIndex(
                name: "ix_charges_previous_reading_id",
                schema: "billing",
                table: "charges",
                column: "previous_reading_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_charges_amount_non_negative",
                schema: "billing",
                table: "charges",
                sql: "amount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_charges_consumption_non_negative",
                schema: "billing",
                table: "charges",
                sql: "consumption >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_accounts_overpayment_non_negative",
                schema: "accounts",
                table: "accounts",
                sql: "overpayment >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_account_tariff_assignments_account_id_effective_from",
                schema: "tariffs",
                table: "account_tariff_assignments",
                columns: new[] { "account_id", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "ix_account_tariff_assignments_tariff_id",
                schema: "tariffs",
                table: "account_tariff_assignments",
                column: "tariff_id");

            migrationBuilder.CreateIndex(
                name: "ix_controller_assignments_address_id",
                schema: "controllers",
                table: "controller_assignments",
                column: "address_id");

            migrationBuilder.CreateIndex(
                name: "ux_controller_assignments_controller_id_address_id",
                schema: "controllers",
                table: "controller_assignments",
                columns: new[] { "controller_id", "address_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_charge_id",
                schema: "payments",
                table: "payment_allocations",
                column: "charge_id");

            migrationBuilder.CreateIndex(
                name: "ux_payment_allocations_payment_id_charge_id",
                schema: "payments",
                table: "payment_allocations",
                columns: new[] { "payment_id", "charge_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_charges_readings_current_reading_id",
                schema: "billing",
                table: "charges",
                column: "current_reading_id",
                principalSchema: "readings",
                principalTable: "meter_readings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_charges_readings_previous_reading_id",
                schema: "billing",
                table: "charges",
                column: "previous_reading_id",
                principalSchema: "readings",
                principalTable: "meter_readings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_meter_readings_meter_readings_supersedes_reading_id",
                schema: "readings",
                table: "meter_readings",
                column: "supersedes_reading_id",
                principalSchema: "readings",
                principalTable: "meter_readings",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_meter_readings_user_accounts_author_user_id",
                schema: "readings",
                table: "meter_readings",
                column: "author_user_id",
                principalSchema: "identity",
                principalTable: "user_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_meters_meters_replaces_meter_id",
                schema: "meters",
                table: "meters",
                column: "replaces_meter_id",
                principalSchema: "meters",
                principalTable: "meters",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_charges_readings_current_reading_id",
                schema: "billing",
                table: "charges");

            migrationBuilder.DropForeignKey(
                name: "fk_charges_readings_previous_reading_id",
                schema: "billing",
                table: "charges");

            migrationBuilder.DropForeignKey(
                name: "fk_meter_readings_meter_readings_supersedes_reading_id",
                schema: "readings",
                table: "meter_readings");

            migrationBuilder.DropForeignKey(
                name: "fk_meter_readings_user_accounts_author_user_id",
                schema: "readings",
                table: "meter_readings");

            migrationBuilder.DropForeignKey(
                name: "fk_meters_meters_replaces_meter_id",
                schema: "meters",
                table: "meters");

            migrationBuilder.DropTable(
                name: "account_tariff_assignments",
                schema: "tariffs");

            migrationBuilder.DropTable(
                name: "controller_assignments",
                schema: "controllers");

            migrationBuilder.DropTable(
                name: "payment_allocations",
                schema: "payments");

            migrationBuilder.DropIndex(
                name: "ux_tariffs_name",
                schema: "tariffs",
                table: "tariffs");

            migrationBuilder.DropIndex(
                name: "ux_meters_replaces_meter_id",
                schema: "meters",
                table: "meters");

            migrationBuilder.DropIndex(
                name: "ux_meters_serial_number",
                schema: "meters",
                table: "meters");

            migrationBuilder.DropIndex(
                name: "ix_meter_readings_author_user_id",
                schema: "readings",
                table: "meter_readings");

            migrationBuilder.DropIndex(
                name: "ux_meter_readings_supersedes_reading_id",
                schema: "readings",
                table: "meter_readings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_meter_readings_source",
                schema: "readings",
                table: "meter_readings");

            migrationBuilder.DropIndex(
                name: "ix_charges_current_reading_id",
                schema: "billing",
                table: "charges");

            migrationBuilder.DropIndex(
                name: "ix_charges_previous_reading_id",
                schema: "billing",
                table: "charges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_charges_amount_non_negative",
                schema: "billing",
                table: "charges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_charges_consumption_non_negative",
                schema: "billing",
                table: "charges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_accounts_overpayment_non_negative",
                schema: "accounts",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "meters",
                table: "meters");

            migrationBuilder.DropColumn(
                name: "replaces_meter_id",
                schema: "meters",
                table: "meters");

            migrationBuilder.DropColumn(
                name: "retired_at",
                schema: "meters",
                table: "meters");

            migrationBuilder.DropColumn(
                name: "author_user_id",
                schema: "readings",
                table: "meter_readings");

            migrationBuilder.DropColumn(
                name: "correction_reason",
                schema: "readings",
                table: "meter_readings");

            migrationBuilder.DropColumn(
                name: "source",
                schema: "readings",
                table: "meter_readings");

            migrationBuilder.DropColumn(
                name: "supersedes_reading_id",
                schema: "readings",
                table: "meter_readings");

            migrationBuilder.DropColumn(
                name: "calculation_version",
                schema: "billing",
                table: "charges");

            migrationBuilder.DropColumn(
                name: "consumption",
                schema: "billing",
                table: "charges");

            migrationBuilder.DropColumn(
                name: "currency",
                schema: "billing",
                table: "charges");

            migrationBuilder.DropColumn(
                name: "current_reading_id",
                schema: "billing",
                table: "charges");

            migrationBuilder.DropColumn(
                name: "previous_reading_id",
                schema: "billing",
                table: "charges");

            migrationBuilder.DropColumn(
                name: "overpayment",
                schema: "accounts",
                table: "accounts");

            migrationBuilder.AlterColumn<string>(
                name: "idempotency_key",
                schema: "payments",
                table: "payments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<decimal>(
                name: "value",
                schema: "readings",
                table: "meter_readings",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                schema: "billing",
                table: "charges",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }
    }
}
