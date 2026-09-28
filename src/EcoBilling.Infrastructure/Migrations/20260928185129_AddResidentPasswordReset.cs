using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResidentPasswordReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "resident_password_reset_operations",
                schema: "residents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_fingerprint = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    resident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resident_password_reset_operations", x => x.id);
                    table.ForeignKey(
                        name: "fk_resident_password_reset_operations_residents_resident_id",
                        column: x => x.resident_id,
                        principalSchema: "residents",
                        principalTable: "residents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_resident_password_reset_operations_resident_id",
                schema: "residents",
                table: "resident_password_reset_operations",
                column: "resident_id");

            migrationBuilder.CreateIndex(
                name: "ux_resident_password_reset_operations_idempotency_key",
                schema: "residents",
                table: "resident_password_reset_operations",
                column: "idempotency_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resident_password_reset_operations",
                schema: "residents");
        }
    }
}
