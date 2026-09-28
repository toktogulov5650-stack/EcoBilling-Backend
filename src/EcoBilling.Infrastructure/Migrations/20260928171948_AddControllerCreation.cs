using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddControllerCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "controller_creation_operations",
                schema: "controllers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_fingerprint = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    controller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_controller_creation_operations", x => x.id);
                    table.ForeignKey(
                        name: "fk_controller_creation_operations_controllers_controller_id",
                        column: x => x.controller_id,
                        principalSchema: "controllers",
                        principalTable: "controllers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_controller_creation_operations_controller_id",
                schema: "controllers",
                table: "controller_creation_operations",
                column: "controller_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_controller_creation_operations_idempotency_key",
                schema: "controllers",
                table: "controller_creation_operations",
                column: "idempotency_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "controller_creation_operations",
                schema: "controllers");
        }
    }
}
