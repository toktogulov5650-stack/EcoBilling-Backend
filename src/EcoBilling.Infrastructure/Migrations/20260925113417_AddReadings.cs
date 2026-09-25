using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "readings");

            migrationBuilder.CreateTable(
                name: "meter_readings",
                schema: "readings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    meter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<decimal>(type: "numeric", nullable: false),
                    measured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meter_readings", x => x.id);
                    table.CheckConstraint("ck_meter_readings_value_non_negative", "value >= 0");
                    table.ForeignKey(
                        name: "fk_meter_readings_meters_meter_id",
                        column: x => x.meter_id,
                        principalSchema: "meters",
                        principalTable: "meters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_meter_readings_meter_id_measured_at",
                schema: "readings",
                table: "meter_readings",
                columns: new[] { "meter_id", "measured_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meter_readings",
                schema: "readings");

            migrationBuilder.DropSchema(
                name: "readings");
        }
    }
}
