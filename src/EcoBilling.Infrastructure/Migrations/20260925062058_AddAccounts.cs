using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "accounts");

            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_number = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_accounts_residents_resident_id",
                        column: x => x.resident_id,
                        principalSchema: "residents",
                        principalTable: "residents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_accounts_resident_id",
                schema: "accounts",
                table: "accounts",
                column: "resident_id");

            migrationBuilder.CreateIndex(
                name: "ux_accounts_account_number",
                schema: "accounts",
                table: "accounts",
                column: "account_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounts",
                schema: "accounts");

            migrationBuilder.DropSchema(
                name: "accounts");
        }
    }
}
