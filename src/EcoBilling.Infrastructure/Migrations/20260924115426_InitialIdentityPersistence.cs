using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoBilling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentityPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "user_accounts",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_type = table.Column<int>(type: "integer", nullable: false),
                    normalized_login = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_accounts", x => x.id);
                    table.CheckConstraint("ck_user_accounts_login_type", "login_type IN (1, 2)");
                    table.CheckConstraint("ck_user_accounts_role", "role IN (1, 2, 3)");
                });

            migrationBuilder.CreateIndex(
                name: "ux_user_accounts_login_type_normalized_login",
                schema: "identity",
                table: "user_accounts",
                columns: new[] { "login_type", "normalized_login" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_accounts",
                schema: "identity");

            migrationBuilder.DropSchema(
                name: "identity");
        }
    }
}
