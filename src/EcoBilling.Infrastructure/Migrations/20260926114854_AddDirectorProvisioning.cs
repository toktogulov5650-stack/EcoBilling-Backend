using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectorProvisioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "infrastructure");

            migrationBuilder.AddColumn<bool>(
                name: "requires_password_change",
                schema: "identity",
                table: "user_accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "directors",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    instance_slot = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_directors", x => x.id);
                    table.CheckConstraint("ck_directors_instance_slot", "instance_slot = 1");
                    table.ForeignKey(
                        name: "fk_directors_user_accounts_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "internal_service_token_replays",
                schema: "infrastructure",
                columns: table => new
                {
                    issuer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    token_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_internal_service_token_replays", x => new { x.issuer, x.token_id });
                });

            migrationBuilder.CreateTable(
                name: "director_provisioning_operations",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    request_fingerprint = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    director_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_director_provisioning_operations", x => x.id);
                    table.ForeignKey(
                        name: "fk_director_provisioning_operations_directors_director_id",
                        column: x => x.director_id,
                        principalSchema: "identity",
                        principalTable: "directors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_user_accounts_single_director",
                schema: "identity",
                table: "user_accounts",
                column: "role",
                unique: true,
                filter: "\"role\" = 3");

            migrationBuilder.CreateIndex(
                name: "ix_director_provisioning_operations_director_id",
                schema: "identity",
                table: "director_provisioning_operations",
                column: "director_id");

            migrationBuilder.CreateIndex(
                name: "ux_director_provisioning_operations_idempotency_key",
                schema: "identity",
                table: "director_provisioning_operations",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_directors_instance_slot",
                schema: "identity",
                table: "directors",
                column: "instance_slot",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_directors_user_id",
                schema: "identity",
                table: "directors",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_internal_service_token_replays_expires_at",
                schema: "infrastructure",
                table: "internal_service_token_replays",
                column: "expires_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "director_provisioning_operations",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "internal_service_token_replays",
                schema: "infrastructure");

            migrationBuilder.DropTable(
                name: "directors",
                schema: "identity");

            migrationBuilder.DropIndex(
                name: "ux_user_accounts_single_director",
                schema: "identity",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "requires_password_change",
                schema: "identity",
                table: "user_accounts");
        }
    }
}
