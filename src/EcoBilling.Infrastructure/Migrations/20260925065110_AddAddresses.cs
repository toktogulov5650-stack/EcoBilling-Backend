using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "addresses",
                schema: "accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    locality = table.Column<string>(type: "text", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    house = table.Column<string>(type: "text", nullable: false),
                    building = table.Column<string>(type: "text", nullable: true),
                    apartment = table.Column<string>(type: "text", nullable: true),
                    search_text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_addresses", x => x.id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "address_id",
                schema: "accounts",
                table: "accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM accounts.accounts) THEN
                        RAISE EXCEPTION
                            'AddAddresses requires accounts.accounts to be empty because no address backfill rule is approved.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "address_id",
                schema: "accounts",
                table: "accounts",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounts_address_id",
                schema: "accounts",
                table: "accounts",
                column: "address_id");

            migrationBuilder.CreateIndex(
                name: "ix_addresses_search_text",
                schema: "accounts",
                table: "addresses",
                column: "search_text");

            migrationBuilder.AddForeignKey(
                name: "fk_accounts_addresses_address_id",
                schema: "accounts",
                table: "accounts",
                column: "address_id",
                principalSchema: "accounts",
                principalTable: "addresses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_accounts_addresses_address_id",
                schema: "accounts",
                table: "accounts");

            migrationBuilder.DropTable(
                name: "addresses",
                schema: "accounts");

            migrationBuilder.DropIndex(
                name: "ix_accounts_address_id",
                schema: "accounts",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "address_id",
                schema: "accounts",
                table: "accounts");
        }
    }
}
