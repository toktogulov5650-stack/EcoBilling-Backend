using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddControllersProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "controllers");

            migrationBuilder.CreateTable(
                name: "controllers",
                schema: "controllers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_controllers", x => x.id);
                    table.ForeignKey(
                        name: "fk_controllers_user_accounts_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_controllers_user_id",
                schema: "controllers",
                table: "controllers",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "controllers",
                schema: "controllers");

            migrationBuilder.DropSchema(
                name: "controllers");
        }
    }
}
