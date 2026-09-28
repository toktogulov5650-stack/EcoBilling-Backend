using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "refresh_sessions",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_session_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_sessions", x => x.id);
                    table.CheckConstraint("ck_refresh_sessions_expiry", "expires_at > created_at");
                    table.CheckConstraint("ck_refresh_sessions_token_hash", "token_hash ~ '^[0-9A-F]{64}$'");
                    table.ForeignKey(
                        name: "fk_refresh_sessions_replacement",
                        column: x => x.replaced_by_session_id,
                        principalSchema: "identity",
                        principalTable: "refresh_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_refresh_sessions_user_accounts_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_sessions_expires_at",
                schema: "identity",
                table: "refresh_sessions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_sessions_family_id",
                schema: "identity",
                table: "refresh_sessions",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_sessions_user_id",
                schema: "identity",
                table: "refresh_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_refresh_sessions_replaced_by_session_id",
                schema: "identity",
                table: "refresh_sessions",
                column: "replaced_by_session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_refresh_sessions_token_hash",
                schema: "identity",
                table: "refresh_sessions",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_sessions",
                schema: "identity");
        }
    }
}
