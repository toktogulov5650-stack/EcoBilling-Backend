using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResidentCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_accounts_resident_id",
                schema: "accounts",
                table: "accounts");

            migrationBuilder.CreateTable(
                name: "resident_creation_operations",
                schema: "residents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_fingerprint = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    resident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resident_creation_operations", x => x.id);
                    table.ForeignKey(
                        name: "fk_resident_creation_operations_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "accounts",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_resident_creation_operations_addresses_address_id",
                        column: x => x.address_id,
                        principalSchema: "accounts",
                        principalTable: "addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_resident_creation_operations_residents_resident_id",
                        column: x => x.resident_id,
                        principalSchema: "residents",
                        principalTable: "residents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_accounts_resident_id",
                schema: "accounts",
                table: "accounts",
                column: "resident_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_resident_creation_operations_account_id",
                schema: "residents",
                table: "resident_creation_operations",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_resident_creation_operations_address_id",
                schema: "residents",
                table: "resident_creation_operations",
                column: "address_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_resident_creation_operations_idempotency_key",
                schema: "residents",
                table: "resident_creation_operations",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_resident_creation_operations_resident_id",
                schema: "residents",
                table: "resident_creation_operations",
                column: "resident_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resident_creation_operations",
                schema: "residents");

            migrationBuilder.DropIndex(
                name: "ux_accounts_resident_id",
                schema: "accounts",
                table: "accounts");

            migrationBuilder.CreateIndex(
                name: "ix_accounts_resident_id",
                schema: "accounts",
                table: "accounts",
                column: "resident_id");
        }
    }
}
