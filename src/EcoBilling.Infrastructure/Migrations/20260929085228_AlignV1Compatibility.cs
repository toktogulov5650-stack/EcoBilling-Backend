using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignV1Compatibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_meters_serial_number",
                schema: "meters",
                table: "meters");

            migrationBuilder.DropCheckConstraint(
                name: "ck_charges_amount_non_negative",
                schema: "billing",
                table: "charges");

            migrationBuilder.AlterColumn<decimal>(
                name: "value",
                schema: "readings",
                table: "meter_readings",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)");

            migrationBuilder.AlterColumn<int>(
                name: "source",
                schema: "readings",
                table: "meter_readings",
                type: "integer",
                nullable: false,
                defaultValue: 2,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                schema: "billing",
                table: "charges",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "value",
                schema: "readings",
                table: "meter_readings",
                type: "numeric(18,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "source",
                schema: "readings",
                table: "meter_readings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                schema: "billing",
                table: "charges",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.CreateIndex(
                name: "ux_meters_serial_number",
                schema: "meters",
                table: "meters",
                column: "serial_number",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_charges_amount_non_negative",
                schema: "billing",
                table: "charges",
                sql: "amount >= 0");
        }
    }
}
