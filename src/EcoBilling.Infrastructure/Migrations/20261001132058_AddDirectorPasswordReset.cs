using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectorPasswordReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "director_password_reset_operations",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_fingerprint = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_director_password_reset_operations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_director_password_reset_operations_idempotency_key",
                schema: "identity",
                table: "director_password_reset_operations",
                column: "idempotency_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "director_password_reset_operations",
                schema: "identity");
        }
    }
}
